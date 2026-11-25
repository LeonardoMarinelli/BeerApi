using System.Text;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BeerApi.Infrastructure.Services;

public sealed class RabbitMqNotificationConsumerService(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqNotificationConsumerService> logger) : BackgroundService
{
    private const string ConsumerName = "notifications";
    private const string RetryCountHeader = "beerapi-retry-count";
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);
                await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    var payload = Encoding.UTF8.GetString(delivery.Body.ToArray());
                    if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var messageId))
                    {
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken);
                        return;
                    }

                    await ProcessAsync(
                        channel,
                        delivery.DeliveryTag,
                        messageId,
                        delivery.BasicProperties.Type ?? string.Empty,
                        payload,
                        delivery.BasicProperties.Headers,
                        stoppingToken);
                };

                await channel.BasicConsumeAsync(RabbitMqTopology.NotificationQueue, false, consumer, cancellationToken: stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "RabbitMQ notification consumer is waiting for the broker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(
        IChannel channel,
        ulong deliveryTag,
        Guid messageId,
        string eventType,
        string payload,
        IDictionary<string, object?>? headers,
        CancellationToken ct)
    {
        ProcessedMessage? processed = null;
        var retryCount = ReadRetryCount(headers);
        var attempt = retryCount + 1;
        var alreadyHandled = false;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            processed = await context.ProcessedMessages.FindAsync([messageId, ConsumerName], ct);
            if (processed?.ProcessedAt is not null || processed?.DeadLetteredAt is not null)
            {
                alreadyHandled = true;
            }
            else
            {
                if (processed is null)
                {
                    processed = new ProcessedMessage { MessageId = messageId, Consumer = ConsumerName };
                    context.ProcessedMessages.Add(processed);
                }

                attempt = Math.Max(processed.Attempts, retryCount) + 1;
                processed.Attempts = attempt;
                await context.SaveChangesAsync(ct);

                var handler = scope.ServiceProvider.GetRequiredService<OrderNotificationHandler>();
                await handler.HandleAsync(messageId, eventType, payload, ct);
                processed.ProcessedAt = DateTimeOffset.UtcNow;
                processed.LastError = null;
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            if (processed is not null)
            {
                processed.Attempts = Math.Max(processed.Attempts, attempt);
                processed.LastError = TruncateError(exception.Message);
                if (processed.Attempts >= MaxAttempts)
                    processed.DeadLetteredAt = DateTimeOffset.UtcNow;

                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var stored = await context.ProcessedMessages.FindAsync([messageId, ConsumerName], ct);
                    if (stored?.ProcessedAt is not null || stored?.DeadLetteredAt is not null)
                    {
                        alreadyHandled = true;
                    }
                    else
                    {
                        if (stored is null)
                            context.ProcessedMessages.Add(processed);
                        else
                        {
                            stored.Attempts = processed.Attempts;
                            stored.LastError = processed.LastError;
                            stored.DeadLetteredAt = processed.DeadLetteredAt;
                        }

                        await context.SaveChangesAsync(ct);
                    }
                }
                catch (Exception persistenceException) when (!ct.IsCancellationRequested)
                {
                    logger.LogError(persistenceException, "Could not persist notification failure for message {MessageId}", messageId);
                }
            }

            logger.LogWarning(exception, "Notification message {MessageId} failed on attempt {Attempt}", messageId, attempt);
            if (alreadyHandled)
            {
                await channel.BasicAckAsync(deliveryTag, false, ct);
                return;
            }

            if (attempt >= MaxAttempts || !IsKnownEventType(eventType))
            {
                await channel.BasicNackAsync(deliveryTag, false, false, ct);
                return;
            }

            await PublishRetryAsync(channel, messageId, eventType, payload, headers, retryCount + 1, ct);
            await channel.BasicAckAsync(deliveryTag, false, ct);
            return;
        }

        await channel.BasicAckAsync(deliveryTag, false, ct);
    }

    private static async Task PublishRetryAsync(
        IChannel channel,
        Guid messageId,
        string eventType,
        string payload,
        IDictionary<string, object?>? headers,
        int retryCount,
        CancellationToken ct)
    {
        var retryHeaders = headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(headers);
        retryHeaders[RetryCountHeader] = retryCount;
        var properties = new BasicProperties
        {
            MessageId = messageId.ToString("N"),
            Type = eventType,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = retryHeaders
        };

        await channel.BasicPublishAsync(
            string.Empty,
            RabbitMqTopology.GetRetryQueue(eventType),
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload),
            cancellationToken: ct);
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCountHeader, out var value))
            return 0;

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var count) => count,
            int count => count,
            long count when count is >= 0 and <= int.MaxValue => (int)count,
            _ => 0
        };
    }

    private static bool IsKnownEventType(string eventType) =>
        eventType is "OrderPlacedEvent" or "OrderStatusChangedEvent" or "StockLowEvent";

    private static string TruncateError(string error) =>
        error.Length <= 2000 ? error : error[..2000];
}

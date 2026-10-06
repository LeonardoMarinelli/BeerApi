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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
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

                    await ProcessAsync(channel, delivery.DeliveryTag, messageId, delivery.BasicProperties.Type ?? string.Empty, payload, stoppingToken);
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
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var processed = await context.ProcessedMessages.FindAsync([messageId, ConsumerName], ct);
        if (processed?.ProcessedAt is not null || processed?.DeadLetteredAt is not null)
        {
            await channel.BasicAckAsync(deliveryTag, false, ct);
            return;
        }

        processed ??= new ProcessedMessage { MessageId = messageId, Consumer = ConsumerName };
        if (context.Entry(processed).State == EntityState.Detached)
            context.ProcessedMessages.Add(processed);
        processed.Attempts++;

        try
        {
            var handler = scope.ServiceProvider.GetRequiredService<OrderNotificationHandler>();
            await handler.HandleAsync(eventType, payload, ct);
            processed.ProcessedAt = DateTimeOffset.UtcNow;
            processed.LastError = null;
            await context.SaveChangesAsync(ct);
            await channel.BasicAckAsync(deliveryTag, false, ct);
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            processed.LastError = exception.Message.Length <= 2000 ? exception.Message : exception.Message[..2000];
            var requeue = processed.Attempts < 3;
            if (!requeue)
                processed.DeadLetteredAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(ct);
            logger.LogWarning(exception, "Notification message {MessageId} failed on attempt {Attempt}", messageId, processed.Attempts);
            if (requeue)
                await Task.Delay(TimeSpan.FromSeconds(processed.Attempts), ct);
            await channel.BasicNackAsync(deliveryTag, false, requeue, ct);
        }
    }
}
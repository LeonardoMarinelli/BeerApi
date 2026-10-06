using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeerApi.Infrastructure.Services;

public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var messages = await context.OutboxMessages
                    .Where(message => message.ProcessedAt == null &&
                                      (message.NextAttemptAt == null || message.NextAttemptAt <= DateTimeOffset.UtcNow))
                    .OrderBy(message => message.OccurredAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await publisher.PublishAsync(message, stoppingToken);
                        message.ProcessedAt = DateTimeOffset.UtcNow;
                        message.LastError = null;
                    }
                    catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        message.Attempts++;
                        message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 8))));
                        message.LastError = exception.Message.Length <= 2000 ? exception.Message : exception.Message[..2000];
                        logger.LogWarning(exception, "Outbox message {MessageId} publish attempt {Attempt} failed", message.Id, message.Attempts);
                    }
                }

                if (messages.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Outbox polling failed");
            }
        }
    }
}
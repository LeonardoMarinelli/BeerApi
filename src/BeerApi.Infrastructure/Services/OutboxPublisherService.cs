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
    private const int MaxAttempts = 10;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextCleanupAt = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTimeOffset.UtcNow;

                if (now >= nextCleanupAt)
                {
                    await CleanupAsync(context, now, stoppingToken);
                    nextCleanupAt = now.AddDays(1);
                }

                await MarkAbandonedMessagesAsync(context, now, stoppingToken);
                var messages = await ClaimMessagesAsync(context, now, stoppingToken);
                foreach (var message in messages)
                    await PublishAsync(context, message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Outbox polling failed");
            }
        }
    }

    private static async Task<List<OutboxMessage>> ClaimMessagesAsync(
        AppDbContext context,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var messages = await context.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT * FROM `OutboxMessages`
                    WHERE ProcessedAt IS NULL
                      AND DeadLetteredAt IS NULL
                      AND Attempts < {MaxAttempts}
                      AND (NextAttemptAt IS NULL OR NextAttemptAt <= {now})
                    ORDER BY OccurredAt
                    LIMIT 50
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                message.Attempts++;
                message.NextAttemptAt = now.Add(LeaseDuration);
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            context.ChangeTracker.Clear();
            return messages;
        });
    }

    private async Task PublishAsync(AppDbContext context, OutboxMessage message, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(message, ct);
            message.ProcessedAt = DateTimeOffset.UtcNow;
            message.NextAttemptAt = null;
            message.LastError = null;
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            message.LastError = TruncateError(exception.Message);
            if (message.Attempts >= MaxAttempts)
            {
                message.DeadLetteredAt = DateTimeOffset.UtcNow;
                message.NextAttemptAt = null;
            }
            else
            {
                message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(
                    Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 8))));
            }

            logger.LogWarning(exception, "Outbox message {MessageId} publish attempt {Attempt} failed", message.Id, message.Attempts);
        }

        context.OutboxMessages.Update(message);
        await context.SaveChangesAsync(ct);
    }

    private static async Task CleanupAsync(AppDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = now.Subtract(RetentionPeriod);
        await context.OutboxMessages
            .Where(message => message.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(ct);
        await context.ProcessedMessages
            .Where(message => message.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    private static async Task MarkAbandonedMessagesAsync(AppDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        await context.OutboxMessages
            .Where(message => message.ProcessedAt == null &&
                              message.DeadLetteredAt == null &&
                              message.Attempts >= MaxAttempts &&
                              (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .ExecuteUpdateAsync(update => update
                .SetProperty(message => message.DeadLetteredAt, now)
                .SetProperty(message => message.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(message => message.LastError, message =>
                    message.LastError ?? "Maximum publish attempts reached before the message was completed."),
                ct);
    }

    private static string TruncateError(string error) =>
        error.Length <= 2000 ? error : error[..2000];
}

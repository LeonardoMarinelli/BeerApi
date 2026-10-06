using BeerApi.Infrastructure.Services.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BeerApi.Api.HealthChecks;

public sealed class RabbitMqHealthCheck(RabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded("RabbitMQ connection is closed.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("RabbitMQ is unavailable; outbox messages will be retried.", exception);
        }
    }
}
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BeerApi.Api.HealthChecks;

public sealed class RedisCacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        const string key = "health-check";

        try
        {
            await cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) },
                cancellationToken);

            return await cache.GetStringAsync(key, cancellationToken) == "ok"
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Redis cache read/write check failed.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}
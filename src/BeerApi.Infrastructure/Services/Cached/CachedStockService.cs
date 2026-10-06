using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedStockService(StockService service, HybridCache cache) : IStockService
{
    public async Task RemoveAsync(int wholesalerId, int beerId, int quantity, CancellationToken ct = default)
    {
        await service.RemoveAsync(wholesalerId, beerId, quantity, ct);
        await cache.RemoveByTagAsync(CacheTags.WholesalerStockFor(wholesalerId), ct);
    }
}
using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedWholesalerService(WholesalerService service, HybridCache cache) : IWholesalerService
{
    public async Task<PagedResultDto<WholesalerDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"wholesalers:list:{page}:{pageSize}",
            token => new ValueTask<PagedResultDto<WholesalerDto>>(service.GetAllAsync(page, pageSize, token)),
            tags: [CacheTags.Wholesalers],
            cancellationToken: ct);

    public async Task<IEnumerable<WholesalerBeerDto>> GetStockByWholesalerIdAsync(int wholesalerId, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"wholesalers:stock:{wholesalerId}",
            token => new ValueTask<IEnumerable<WholesalerBeerDto>>(service.GetStockByWholesalerIdAsync(wholesalerId, token)),
            tags: [CacheTags.WholesalerStock, CacheTags.WholesalerStockFor(wholesalerId)],
            cancellationToken: ct);

    public Task<QuoteResponseDto> GetQuoteAsync(int wholesalerId, QuoteRequestDto request, CancellationToken ct = default) =>
        service.GetQuoteAsync(wholesalerId, request, ct);
}
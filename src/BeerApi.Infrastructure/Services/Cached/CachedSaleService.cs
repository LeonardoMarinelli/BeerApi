using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedSaleService(SaleService service, HybridCache cache) : ISaleService
{
    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, CancellationToken ct = default)
    {
        var sale = await service.CreateSaleAsync(dto, ct);
        await cache.RemoveByTagAsync(CacheTags.WholesalerStockFor(dto.WholesalerId), ct);
        return sale;
    }

    public Task<PagedResultDto<SaleDto>> GetAllAsync(int page, int pageSize, int? breweryId, CancellationToken ct = default) =>
        service.GetAllAsync(page, pageSize, breweryId, ct);
}
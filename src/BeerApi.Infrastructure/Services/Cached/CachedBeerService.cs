using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedBeerService(BeerService service, HybridCache cache) : IBeerService
{
    public async Task<IEnumerable<BeerDto>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"beers:brewery:{breweryId}",
            token => new ValueTask<IEnumerable<BeerDto>>(service.GetByBreweryIdAsync(breweryId, token)),
            tags: [CacheTags.BreweryBeers(breweryId)],
            cancellationToken: ct);

    public Task<PagedResultDto<BeerDto>> SearchAsync(BeerSearchFiltersDto filters, int page, int pageSize, CancellationToken ct = default) =>
        service.SearchAsync(filters, page, pageSize, ct);

    public Task<CursorPageResultDto<BeerDto>> SearchCursorAsync(BeerSearchFiltersDto filters, int pageSize, string? cursor, CancellationToken ct = default) =>
        service.SearchCursorAsync(filters, pageSize, cursor, ct);

    public async Task<BeerDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"beers:item:{id}",
            token => new ValueTask<BeerDto>(service.GetByIdAsync(id, token)),
            tags: [CacheTags.Beer(id)],
            cancellationToken: ct);

    public async Task<BeerDto> CreateAsync(int breweryId, CreateBeerDto dto, CancellationToken ct = default)
    {
        var beer = await service.CreateAsync(breweryId, dto, ct);
        await InvalidateAsync(breweryId, beer.Id, ct);
        return beer;
    }

    public async Task<BeerDto> UpdateAsync(int breweryId, int beerId, UpdateBeerDto dto, CancellationToken ct = default)
    {
        var beer = await service.UpdateAsync(breweryId, beerId, dto, ct);
        await InvalidateAsync(breweryId, beerId, ct);
        return beer;
    }

    public async Task DeleteAsync(int breweryId, int beerId, CancellationToken ct = default)
    {
        await service.DeleteAsync(breweryId, beerId, ct);
        await InvalidateAsync(breweryId, beerId, ct);
    }

    private async Task InvalidateAsync(int breweryId, int beerId, CancellationToken ct)
    {
        await cache.RemoveByTagAsync(CacheTags.BreweryBeers(breweryId), ct);
        await cache.RemoveByTagAsync(CacheTags.Beer(beerId), ct);
        await cache.RemoveByTagAsync(CacheTags.WholesalerStock, ct);
    }
}
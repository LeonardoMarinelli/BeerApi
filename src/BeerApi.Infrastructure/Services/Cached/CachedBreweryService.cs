using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedBreweryService(BreweryService service, HybridCache cache) : IBreweryService
{
    public async Task<PagedResultDto<BreweryDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"breweries:list:{page}:{pageSize}",
            token => new ValueTask<PagedResultDto<BreweryDto>>(service.GetAllAsync(page, pageSize, token)),
            tags: [CacheTags.Breweries],
            cancellationToken: ct);

    public async Task<BreweryDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            $"breweries:item:{id}",
            token => new ValueTask<BreweryDto>(service.GetByIdAsync(id, token)),
            tags: [CacheTags.Breweries],
            cancellationToken: ct);
}
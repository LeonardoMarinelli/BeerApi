using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class BreweryService(IBreweryRepository repository) : IBreweryService
{
    private readonly IBreweryRepository _repository = repository;

    public async Task<PagedResultDto<BreweryDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (breweries, totalCount) = await _repository.GetAllAsync(page, pageSize, ct);
        var items = breweries.Select(b => new BreweryDto(b.Id, b.Name, b.Country, b.Description));
        return new PagedResultDto<BreweryDto>(items, page, pageSize, totalCount);
    }

    public async Task<BreweryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var brewery = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cervejaria", id);
        return new BreweryDto(brewery.Id, brewery.Name, brewery.Country, brewery.Description);
    }
}

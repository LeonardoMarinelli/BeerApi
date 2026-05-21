using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IBeerService
{
    Task<IEnumerable<BeerDto>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default);
    Task<BeerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BeerDto> CreateAsync(int breweryId, CreateBeerDto dto, CancellationToken ct = default);
    Task<BeerDto> UpdateAsync(int breweryId, int beerId, UpdateBeerDto dto, CancellationToken ct = default);
    Task DeleteAsync(int breweryId, int beerId, CancellationToken ct = default);
}

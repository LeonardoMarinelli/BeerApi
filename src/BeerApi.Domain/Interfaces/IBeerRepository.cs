using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IBeerRepository
{
    Task<IEnumerable<Beer>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default);
    Task<Beer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Beer?> GetByIdWithBreweryAsync(int id, CancellationToken ct = default);
    Task AddAsync(Beer beer, CancellationToken ct = default);
    Task UpdateAsync(Beer beer, CancellationToken ct = default);
    Task DeleteAsync(Beer beer, CancellationToken ct = default);
}

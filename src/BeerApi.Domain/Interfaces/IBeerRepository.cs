using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IBeerRepository
{
    Task<IEnumerable<Beer>> GetByBreweryIdAsync(int breweryId);
    Task<Beer?> GetByIdAsync(int id);
    Task<Beer?> GetByIdWithBreweryAsync(int id);
    Task AddAsync(Beer beer);
    Task UpdateAsync(Beer beer);
    Task DeleteAsync(Beer beer);
}

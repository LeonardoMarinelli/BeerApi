using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IBreweryRepository
{
    Task<IEnumerable<Brewery>> GetAllAsync();
    Task<Brewery?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
}

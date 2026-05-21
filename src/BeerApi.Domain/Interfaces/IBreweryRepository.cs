using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IBreweryRepository
{
    Task<IEnumerable<Brewery>> GetAllAsync(CancellationToken ct = default);
    Task<Brewery?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

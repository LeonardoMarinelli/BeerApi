using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IWholesalerRepository
{
    Task<IEnumerable<Wholesaler>> GetAllAsync(CancellationToken ct = default);
    Task<Wholesaler?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Wholesaler?> GetByIdWithStockAsync(int id, CancellationToken ct = default);
    Task<WholesalerBeer?> GetStockEntryAsync(int wholesalerId, int beerId, CancellationToken ct = default);
    Task AddStockEntryAsync(WholesalerBeer entry, CancellationToken ct = default);
    Task UpdateStockEntryAsync(WholesalerBeer entry, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

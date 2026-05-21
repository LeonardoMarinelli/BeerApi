using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface IWholesalerRepository
{
    Task<IEnumerable<Wholesaler>> GetAllAsync();
    Task<Wholesaler?> GetByIdAsync(int id);
    Task<Wholesaler?> GetByIdWithStockAsync(int id);
    Task<WholesalerBeer?> GetStockEntryAsync(int wholesalerId, int beerId);
    Task AddStockEntryAsync(WholesalerBeer entry);
    Task UpdateStockEntryAsync(WholesalerBeer entry);
    Task<bool> ExistsAsync(int id);
}

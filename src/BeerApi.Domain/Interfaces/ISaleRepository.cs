using BeerApi.Domain.Entities;

namespace BeerApi.Domain.Interfaces;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct = default);
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(int page, int pageSize, int? breweryId, CancellationToken ct = default);
}

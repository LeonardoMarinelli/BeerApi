using BeerApi.Domain.Entities;
using BeerApi.Domain.Queries;

namespace BeerApi.Domain.Interfaces;

public interface IBeerRepository
{
    Task<IEnumerable<Beer>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default);
    Task<IReadOnlyList<Beer>> GetByIdsWithBreweryAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<Beer> Items, int TotalCount)> SearchAsync(BeerSearchCriteria criteria, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Beer>> SearchCursorAsync(BeerSearchCriteria criteria, BeerCursorPosition? after, int take, CancellationToken ct = default);
    Task<Beer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Beer?> GetByIdWithBreweryAsync(int id, CancellationToken ct = default);
    Task AddAsync(Beer beer, CancellationToken ct = default);
    Task UpdateAsync(Beer beer, CancellationToken ct = default);
    Task DeleteAsync(Beer beer, CancellationToken ct = default);
}

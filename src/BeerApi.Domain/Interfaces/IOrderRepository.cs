using BeerApi.Domain.Entities;
using BeerApi.Domain.Queries;

namespace BeerApi.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(
        OrderSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Queries;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public sealed class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        context.Entry(order.Brewery).State = EntityState.Unchanged;
        context.Entry(order.Wholesaler).State = EntityState.Unchanged;
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Orders
            .Include(order => order.Brewery)
            .Include(order => order.Wholesaler)
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, ct);

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        context.Orders.Update(order);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O pedido foi atualizado por outra operação. Recarregue e tente novamente.");
        }
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(
        OrderSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Orders.AsNoTracking()
            .Include(order => order.Brewery)
            .Include(order => order.Wholesaler)
            .Include(order => order.Items)
            .AsQueryable();

        if (criteria.BreweryId.HasValue)
            query = query.Where(order => order.BreweryId == criteria.BreweryId.Value);
        if (criteria.WholesalerId.HasValue)
            query = query.Where(order => order.WholesalerId == criteria.WholesalerId.Value);
        if (criteria.Status.HasValue)
            query = query.Where(order => order.Status == criteria.Status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }
}
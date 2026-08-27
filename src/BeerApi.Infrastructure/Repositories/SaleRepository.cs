using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class SaleRepository(AppDbContext context) : ISaleRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(int page, int pageSize, int? breweryId, CancellationToken ct = default)
    {
        var query = _context.Sales.AsNoTracking()
            .Include(s => s.Beer)
            .Include(s => s.Wholesaler)
            .AsQueryable();

        if (breweryId.HasValue)
            query = query.Where(s => s.BreweryId == breweryId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}

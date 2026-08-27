using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class BreweryRepository(AppDbContext context) : IBreweryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<(IEnumerable<Brewery> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Breweries.AsNoTracking().OrderByDescending(b => b.Id);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Brewery?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _context.Breweries.FindAsync([id], ct);

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await _context.Breweries.AnyAsync(b => b.Id == id, ct);
}

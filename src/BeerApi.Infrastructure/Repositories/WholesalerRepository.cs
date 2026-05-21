using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class WholesalerRepository(AppDbContext context) : IWholesalerRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Wholesaler>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Wholesalers.AsNoTracking().ToListAsync(ct);

    public async Task<Wholesaler?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _context.Wholesalers.FindAsync([id], ct);

    public async Task<Wholesaler?> GetByIdWithStockAsync(int id, CancellationToken ct = default) =>
        await _context.Wholesalers
            .Include(w => w.WholesalerBeers)
                .ThenInclude(wb => wb.Beer)
                    .ThenInclude(b => b.Brewery)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<WholesalerBeer?> GetStockEntryAsync(int wholesalerId, int beerId, CancellationToken ct = default) =>
        await _context.WholesalerBeers
            .FirstOrDefaultAsync(wb => wb.WholesalerId == wholesalerId && wb.BeerId == beerId, ct);

    public async Task AddStockEntryAsync(WholesalerBeer entry, CancellationToken ct = default)
    {
        _context.WholesalerBeers.Add(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateStockEntryAsync(WholesalerBeer entry, CancellationToken ct = default)
    {
        _context.WholesalerBeers.Update(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await _context.Wholesalers.AnyAsync(w => w.Id == id, ct);
}

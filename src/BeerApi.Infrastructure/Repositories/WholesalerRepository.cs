using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class WholesalerRepository(AppDbContext context) : IWholesalerRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Wholesaler>> GetAllAsync() =>
        await _context.Wholesalers.AsNoTracking().ToListAsync();

    public async Task<Wholesaler?> GetByIdAsync(int id) =>
        await _context.Wholesalers.FindAsync(id);

    public async Task<Wholesaler?> GetByIdWithStockAsync(int id) =>
        await _context.Wholesalers
            .Include(w => w.WholesalerBeers)
                .ThenInclude(wb => wb.Beer)
                    .ThenInclude(b => b.Brewery)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task<WholesalerBeer?> GetStockEntryAsync(int wholesalerId, int beerId) =>
        await _context.WholesalerBeers
            .FirstOrDefaultAsync(wb => wb.WholesalerId == wholesalerId && wb.BeerId == beerId);

    public async Task AddStockEntryAsync(WholesalerBeer entry)
    {
        _context.WholesalerBeers.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStockEntryAsync(WholesalerBeer entry)
    {
        _context.WholesalerBeers.Update(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Wholesalers.AnyAsync(w => w.Id == id);
}

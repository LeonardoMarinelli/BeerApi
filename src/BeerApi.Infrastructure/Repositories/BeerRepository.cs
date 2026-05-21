using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class BeerRepository(AppDbContext context) : IBeerRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Beer>> GetByBreweryIdAsync(int breweryId, CancellationToken ct = default) =>
        await _context.Beers
            .AsNoTracking()
            .Include(b => b.Brewery)
            .Where(b => b.BreweryId == breweryId)
            .ToListAsync(ct);

    public async Task<Beer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _context.Beers.FindAsync([id], ct);

    public async Task<Beer?> GetByIdWithBreweryAsync(int id, CancellationToken ct = default) =>
        await _context.Beers
            .Include(b => b.Brewery)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(Beer beer, CancellationToken ct = default)
    {
        _context.Beers.Add(beer);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Beer beer, CancellationToken ct = default)
    {
        _context.Beers.Update(beer);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Beer beer, CancellationToken ct = default)
    {
        _context.Beers.Remove(beer);
        await _context.SaveChangesAsync(ct);
    }
}

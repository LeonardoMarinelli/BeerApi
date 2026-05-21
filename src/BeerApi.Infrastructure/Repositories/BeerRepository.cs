using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class BeerRepository(AppDbContext context) : IBeerRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Beer>> GetByBreweryIdAsync(int breweryId) =>
        await _context.Beers
            .AsNoTracking()
            .Include(b => b.Brewery)
            .Where(b => b.BreweryId == breweryId)
            .ToListAsync();

    public async Task<Beer?> GetByIdAsync(int id) =>
        await _context.Beers.FindAsync(id);

    public async Task<Beer?> GetByIdWithBreweryAsync(int id) =>
        await _context.Beers
            .Include(b => b.Brewery)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(Beer beer)
    {
        _context.Beers.Add(beer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Beer beer)
    {
        _context.Beers.Update(beer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Beer beer)
    {
        _context.Beers.Remove(beer);
        await _context.SaveChangesAsync();
    }
}

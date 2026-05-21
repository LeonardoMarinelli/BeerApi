using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class BreweryRepository(AppDbContext context) : IBreweryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Brewery>> GetAllAsync() =>
        await _context.Breweries.AsNoTracking().ToListAsync();

    public async Task<Brewery?> GetByIdAsync(int id) =>
        await _context.Breweries.FindAsync(id);

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Breweries.AnyAsync(b => b.Id == id);
}

using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public class BreweryRepository(AppDbContext context) : IBreweryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Brewery>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Breweries.AsNoTracking().ToListAsync(ct);

    public async Task<Brewery?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _context.Breweries.FindAsync([id], ct);

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await _context.Breweries.AnyAsync(b => b.Id == id, ct);
}

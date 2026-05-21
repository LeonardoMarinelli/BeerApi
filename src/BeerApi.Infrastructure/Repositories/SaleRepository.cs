using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Infrastructure.Data;

namespace BeerApi.Infrastructure.Repositories;

public class SaleRepository(AppDbContext context) : ISaleRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(ct);
    }
}

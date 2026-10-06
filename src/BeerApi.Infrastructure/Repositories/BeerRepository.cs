using BeerApi.Domain.Entities;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Queries;
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

    public async Task<IReadOnlyList<Beer>> GetByIdsWithBreweryAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await _context.Beers
            .AsNoTracking()
            .Include(beer => beer.Brewery)
            .Where(beer => ids.Contains(beer.Id))
            .ToListAsync(ct);

    public async Task<(IEnumerable<Beer> Items, int TotalCount)> SearchAsync(
        BeerSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(_context.Beers.AsNoTracking().Include(beer => beer.Brewery), criteria);
        var totalCount = await query.CountAsync(ct);
        var items = await ApplySort(query, criteria)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<IEnumerable<Beer>> SearchCursorAsync(
        BeerSearchCriteria criteria,
        BeerCursorPosition? after,
        int take,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(_context.Beers.AsNoTracking().Include(beer => beer.Brewery), criteria);
        if (after is not null)
        {
            query = (criteria.SortBy, criteria.SortDirection) switch
            {
                (BeerSortField.Id, SortDirection.Ascending) => query.Where(beer => beer.Id > after.BeerId),
                (BeerSortField.Id, SortDirection.Descending) => query.Where(beer => beer.Id < after.BeerId),
                (BeerSortField.Price, SortDirection.Ascending) => query.Where(beer =>
                    beer.Price > after.SortValue || (beer.Price == after.SortValue && beer.Id > after.BeerId)),
                (BeerSortField.Price, SortDirection.Descending) => query.Where(beer =>
                    beer.Price < after.SortValue || (beer.Price == after.SortValue && beer.Id < after.BeerId)),
                (BeerSortField.AlcoholContent, SortDirection.Ascending) => query.Where(beer =>
                    beer.AlcoholContent > after.SortValue || (beer.AlcoholContent == after.SortValue && beer.Id > after.BeerId)),
                (BeerSortField.AlcoholContent, SortDirection.Descending) => query.Where(beer =>
                    beer.AlcoholContent < after.SortValue || (beer.AlcoholContent == after.SortValue && beer.Id < after.BeerId)),
                _ => throw new ArgumentOutOfRangeException(nameof(criteria))
            };
        }

        return await ApplySort(query, criteria).Take(take).ToListAsync(ct);
    }

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

    private static IQueryable<Beer> ApplyFilters(IQueryable<Beer> query, BeerSearchCriteria criteria)
    {
        if (criteria.Query is not null)
            query = query.Where(beer =>
                beer.Name.Contains(criteria.Query) ||
                beer.Description.Contains(criteria.Query) ||
                beer.Brewery.Name.Contains(criteria.Query));
        if (criteria.BreweryId.HasValue)
            query = query.Where(beer => beer.BreweryId == criteria.BreweryId.Value);
        if (criteria.Style.HasValue)
            query = query.Where(beer => beer.Style == criteria.Style.Value);
        if (criteria.MinAbv.HasValue)
            query = query.Where(beer => beer.AlcoholContent >= criteria.MinAbv.Value);
        if (criteria.MaxAbv.HasValue)
            query = query.Where(beer => beer.AlcoholContent <= criteria.MaxAbv.Value);
        if (criteria.MinPrice.HasValue)
            query = query.Where(beer => beer.Price >= criteria.MinPrice.Value);
        if (criteria.MaxPrice.HasValue)
            query = query.Where(beer => beer.Price <= criteria.MaxPrice.Value);

        return query;
    }

    private static IOrderedQueryable<Beer> ApplySort(IQueryable<Beer> query, BeerSearchCriteria criteria) =>
        (criteria.SortBy, criteria.SortDirection) switch
        {
            (BeerSortField.Id, SortDirection.Ascending) => query.OrderBy(beer => beer.Id),
            (BeerSortField.Id, SortDirection.Descending) => query.OrderByDescending(beer => beer.Id),
            (BeerSortField.Name, SortDirection.Ascending) => query.OrderBy(beer => beer.Name).ThenBy(beer => beer.Id),
            (BeerSortField.Name, SortDirection.Descending) => query.OrderByDescending(beer => beer.Name).ThenByDescending(beer => beer.Id),
            (BeerSortField.Price, SortDirection.Ascending) => query.OrderBy(beer => beer.Price).ThenBy(beer => beer.Id),
            (BeerSortField.Price, SortDirection.Descending) => query.OrderByDescending(beer => beer.Price).ThenByDescending(beer => beer.Id),
            (BeerSortField.AlcoholContent, SortDirection.Ascending) => query.OrderBy(beer => beer.AlcoholContent).ThenBy(beer => beer.Id),
            (BeerSortField.AlcoholContent, SortDirection.Descending) => query.OrderByDescending(beer => beer.AlcoholContent).ThenByDescending(beer => beer.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria))
        };
}

using BeerApi.Domain.Entities;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Reports;
using BeerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Infrastructure.Repositories;

public sealed class ReportRepository(AppDbContext context) : IReportRepository
{
    public async Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        bool groupByMonth,
        int? breweryId,
        CancellationToken ct = default)
    {
        var query = context.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= fromInclusive && sale.SaleDate < toExclusive);
        if (breweryId.HasValue)
            query = query.Where(sale => sale.BreweryId == breweryId.Value);

        if (groupByMonth)
        {
            var grouped = await query.GroupBy(sale => new { sale.SaleDate.Year, sale.SaleDate.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    SalesCount = group.Count(),
                    Quantity = group.Sum(sale => (long)sale.Quantity),
                    Revenue = group.Sum(sale => sale.TotalPrice)
                })
                .OrderBy(row => row.Year)
                .ThenBy(row => row.Month)
                .ToListAsync(ct);

            return grouped.Select(row => new SalesReportRow(
                new DateTime(row.Year, row.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                row.SalesCount,
                row.Quantity,
                row.Revenue)).ToList();
        }

        var daily = await query.GroupBy(sale => sale.SaleDate.Date)
            .Select(group => new
            {
                PeriodStart = group.Key,
                SalesCount = group.Count(),
                Quantity = group.Sum(sale => (long)sale.Quantity),
                Revenue = group.Sum(sale => sale.TotalPrice)
            })
            .OrderBy(row => row.PeriodStart)
            .ToListAsync(ct);

        return daily.Select(row => new SalesReportRow(
            DateTime.SpecifyKind(row.PeriodStart, DateTimeKind.Utc),
            row.SalesCount,
            row.Quantity,
            row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<TopBeerReportRow>> GetTopBeersAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        int? breweryId,
        int limit,
        CancellationToken ct = default)
    {
        var query = context.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= fromInclusive && sale.SaleDate < toExclusive);
        if (breweryId.HasValue)
            query = query.Where(sale => sale.BreweryId == breweryId.Value);

        var grouped = await query.GroupBy(sale => new { sale.BeerId, sale.Beer.Name })
            .Select(group => new
            {
                group.Key.BeerId,
                BeerName = group.Key.Name,
                Quantity = group.Sum(sale => (long)sale.Quantity),
                Revenue = group.Sum(sale => sale.TotalPrice)
            })
            .OrderByDescending(row => row.Quantity)
            .ThenBy(row => row.BeerId)
            .Take(limit)
            .ToListAsync(ct);

        return grouped.Select(row => new TopBeerReportRow(
            row.BeerId, row.BeerName, row.Quantity, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<StockReportRow>> GetStockAsync(
        int? wholesalerId,
        bool lowOnly,
        int lowThreshold,
        CancellationToken ct = default)
    {
        var query = context.WholesalerBeers.AsNoTracking().AsQueryable();
        if (wholesalerId.HasValue)
            query = query.Where(entry => entry.WholesalerId == wholesalerId.Value);
        if (lowOnly)
            query = query.Where(entry => entry.Quantity <= lowThreshold);

        return await query
            .OrderBy(entry => entry.WholesalerId)
            .ThenBy(entry => entry.BeerId)
            .Select(entry => new StockReportRow(
                entry.WholesalerId,
                entry.Wholesaler.Name,
                entry.BeerId,
                entry.Beer.Name,
                entry.Quantity,
                entry.Quantity <= lowThreshold))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderStatusReportRow>> GetOrdersByStatusAsync(
        int? breweryId,
        int? wholesalerId,
        CancellationToken ct = default)
    {
        var query = context.Orders.AsNoTracking().AsQueryable();
        if (breweryId.HasValue)
            query = query.Where(order => order.BreweryId == breweryId.Value);
        if (wholesalerId.HasValue)
            query = query.Where(order => order.WholesalerId == wholesalerId.Value);

        return await query.GroupBy(order => order.Status)
            .Select(group => new OrderStatusReportRow(group.Key, group.Count()))
            .ToListAsync(ct);
    }
}
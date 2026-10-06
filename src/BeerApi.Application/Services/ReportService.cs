using BeerApi.Application.DTOs;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public sealed class ReportService(IReportRepository repository, int lowStockThreshold)
{
    public async Task<IReadOnlyList<SalesReportDto>> GetSalesAsync(
        DateOnly? from,
        DateOnly? to,
        string groupBy,
        int? breweryId,
        CancellationToken ct = default)
    {
        var range = ResolveRange(from, to);
        var monthly = groupBy.Trim().ToLowerInvariant() switch
        {
            "day" => false,
            "month" => true,
            _ => throw new BusinessException("groupBy deve ser day ou month.")
        };
        var rows = await repository.GetSalesAsync(range.From, range.ToExclusive, monthly, breweryId, ct);
        return rows.Select(row => new SalesReportDto(row.PeriodStart, row.SalesCount, row.Quantity, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<TopBeerReportDto>> GetTopBeersAsync(
        DateOnly? from,
        DateOnly? to,
        int limit,
        int? breweryId,
        CancellationToken ct = default)
    {
        if (limit is < 1 or > 50)
            throw new BusinessException("limit deve estar entre 1 e 50.");
        var range = ResolveRange(from, to);
        var rows = await repository.GetTopBeersAsync(range.From, range.ToExclusive, breweryId, limit, ct);
        return rows.Select(row => new TopBeerReportDto(row.BeerId, row.BeerName, row.Quantity, row.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<StockReportDto>> GetStockAsync(
        int? wholesalerId,
        bool lowOnly,
        CancellationToken ct = default)
    {
        var rows = await repository.GetStockAsync(wholesalerId, lowOnly, lowStockThreshold, ct);
        return rows.Select(row => new StockReportDto(
            row.WholesalerId, row.WholesalerName, row.BeerId, row.BeerName, row.Quantity, row.IsLow)).ToList();
    }

    public async Task<IReadOnlyList<OrdersByStatusReportDto>> GetOrdersByStatusAsync(
        int? breweryId,
        int? wholesalerId,
        CancellationToken ct = default)
    {
        var rows = await repository.GetOrdersByStatusAsync(breweryId, wholesalerId, ct);
        return Enum.GetValues<Domain.Enums.OrderStatus>()
            .Select(status => new OrdersByStatusReportDto(status, rows.FirstOrDefault(row => row.Status == status)?.Count ?? 0))
            .ToList();
    }

    private static (DateTime From, DateTime ToExclusive) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? today.AddDays(-29);
        var end = to ?? today;
        if (start > end || end.DayNumber - start.DayNumber >= 366)
            throw new BusinessException("O período deve ser válido e conter no máximo 366 dias.");

        return (
            start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }
}
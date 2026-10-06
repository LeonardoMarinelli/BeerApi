using BeerApi.Domain.Reports;

namespace BeerApi.Domain.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(DateTime fromInclusive, DateTime toExclusive, bool groupByMonth, int? breweryId, CancellationToken ct = default);
    Task<IReadOnlyList<TopBeerReportRow>> GetTopBeersAsync(DateTime fromInclusive, DateTime toExclusive, int? breweryId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<StockReportRow>> GetStockAsync(int? wholesalerId, bool lowOnly, int lowThreshold, CancellationToken ct = default);
    Task<IReadOnlyList<OrderStatusReportRow>> GetOrdersByStatusAsync(int? breweryId, int? wholesalerId, CancellationToken ct = default);
}
using BeerApi.Domain.Enums;

namespace BeerApi.Domain.Reports;

public sealed record SalesReportRow(DateTime PeriodStart, int SalesCount, long Quantity, decimal Revenue);
public sealed record TopBeerReportRow(int BeerId, string BeerName, long Quantity, decimal Revenue);
public sealed record StockReportRow(int WholesalerId, string WholesalerName, int BeerId, string BeerName, int Quantity, bool IsLow);
public sealed record OrderStatusReportRow(OrderStatus Status, int Count);
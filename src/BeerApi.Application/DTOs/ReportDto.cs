using System.ComponentModel.DataAnnotations;
using BeerApi.Domain.Enums;

namespace BeerApi.Application.DTOs;

public sealed record SalesReportQueryDto
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    [MaxLength(10)] public string GroupBy { get; init; } = "day";
    [MaxLength(10)] public string Format { get; init; } = "json";
    public int? BreweryId { get; init; }
}

public sealed record TopBeersReportQueryDto
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    [Range(1, 50)] public int Limit { get; init; } = 10;
    [MaxLength(10)] public string Format { get; init; } = "json";
    public int? BreweryId { get; init; }
}

public sealed record StockReportQueryDto
{
    public int? WholesalerId { get; init; }
    public bool LowOnly { get; init; }
    [MaxLength(10)] public string Format { get; init; } = "json";
}

public sealed record OrdersByStatusReportQueryDto
{
    public int? BreweryId { get; init; }
    public int? WholesalerId { get; init; }
    [MaxLength(10)] public string Format { get; init; } = "json";
}

public sealed record SalesReportDto(DateTime PeriodStartUtc, int SalesCount, long Quantity, decimal Revenue);
public sealed record TopBeerReportDto(int BeerId, string BeerName, long Quantity, decimal Revenue);
public sealed record StockReportDto(int WholesalerId, string WholesalerName, int BeerId, string BeerName, int Quantity, bool IsLow);
public sealed record OrdersByStatusReportDto(OrderStatus Status, int Count);
using BeerApi.Domain.Enums;

namespace BeerApi.Domain.Queries;

public enum BeerSortField
{
    Id,
    Name,
    Price,
    AlcoholContent
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record BeerSearchCriteria(
    string? Query,
    int? BreweryId,
    BeerStyle? Style,
    decimal? MinAbv,
    decimal? MaxAbv,
    decimal? MinPrice,
    decimal? MaxPrice,
    BeerSortField SortBy,
    SortDirection SortDirection);

public sealed record BeerCursorPosition(decimal SortValue, int BeerId);
using System.ComponentModel.DataAnnotations;
using BeerApi.Domain.Enums;

namespace BeerApi.Application.DTOs;

public record BeerSearchFiltersDto
{
    [MaxLength(100)]
    public string? Q { get; init; }

    [Range(1, int.MaxValue)]
    public int? BreweryId { get; init; }

    public BeerStyle? Style { get; init; }

    [Range(0.1, 100.0)]
    public decimal? MinAbv { get; init; }

    [Range(0.1, 100.0)]
    public decimal? MaxAbv { get; init; }

    [Range(0.01, 99999.99)]
    public decimal? MinPrice { get; init; }

    [Range(0.01, 99999.99)]
    public decimal? MaxPrice { get; init; }

    public string? SortBy { get; init; } = "id";
    public string? SortDir { get; init; } = "asc";
}

public sealed record BeerSearchRequestDto : BeerSearchFiltersDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record BeerCursorSearchRequestDto : BeerSearchFiltersDto
{
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(2048)]
    public string? Cursor { get; init; }
}
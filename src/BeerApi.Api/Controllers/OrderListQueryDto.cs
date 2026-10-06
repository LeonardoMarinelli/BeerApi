using System.ComponentModel.DataAnnotations;
using BeerApi.Domain.Enums;

namespace BeerApi.Api.Controllers;

public sealed record OrderListQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public OrderStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int? BreweryId { get; init; }

    [Range(1, int.MaxValue)]
    public int? WholesalerId { get; init; }
}
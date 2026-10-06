using System.ComponentModel.DataAnnotations;
using BeerApi.Domain.Enums;

namespace BeerApi.Application.DTOs;

public sealed record CreateOrderItemDto(
    [Range(1, int.MaxValue)] int BeerId,
    [Range(1, 1_000_000)] int Quantity);

public sealed record CreateOrderDto(
    int? WholesalerId,
    [Required, MinLength(1), MaxLength(50)] List<CreateOrderItemDto> Items);

public sealed record CancelOrderRequestDto([MaxLength(500)] string? Reason);

public sealed record OrderSearchFiltersDto(int? BreweryId, int? WholesalerId, OrderStatus? Status);

public sealed record OrderItemDto(
    int BeerId,
    string BeerName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal DiscountedSubtotal);

public sealed record OrderDto(
    int Id,
    int BreweryId,
    string BreweryName,
    int WholesalerId,
    string WholesalerName,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    string? CancelReason,
    decimal TotalBeforeDiscount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TotalPrice,
    IReadOnlyList<OrderItemDto> Items);
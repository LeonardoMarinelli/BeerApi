using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public record QuoteRequestDto([Required, MinLength(1)] List<QuoteItemRequestDto> Items);

public record QuoteItemRequestDto(
    [Range(1, int.MaxValue)] int BeerId,
    [Range(1, 1_000_000)] int Quantity);

public record QuoteResponseDto(
    string WholesalerName,
    List<QuoteItemResponseDto> Items,
    decimal TotalBeforeDiscount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalPrice);

public record QuoteItemResponseDto(
    string BeerName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

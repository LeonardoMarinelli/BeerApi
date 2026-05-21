using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public record SaleDto(
    int Id,
    int BeerId,
    string BeerName,
    int WholesalerId,
    string WholesalerName,
    int Quantity,
    decimal PricePerUnit,
    decimal TotalPrice,
    decimal TaxRate,
    DateTime SaleDate);

public record CreateSaleDto(
    [Range(1, int.MaxValue)] int BeerId,
    [Range(1, int.MaxValue)] int WholesalerId,
    [Range(1, 1_000_000)] int Quantity);

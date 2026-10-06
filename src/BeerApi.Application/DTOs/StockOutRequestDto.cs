using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public sealed record StockOutRequestDto([Range(1, 1_000_000)] int Quantity);
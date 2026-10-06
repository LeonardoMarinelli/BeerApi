using System.ComponentModel.DataAnnotations;
using BeerApi.Domain.Enums;

namespace BeerApi.Application.DTOs;

public record BeerDto(
    int Id,
    string Name,
    string Description,
    decimal AlcoholContent,
    decimal Price,
    int BreweryId,
    string BreweryName,
    BeerStyle Style = BeerStyle.Other);

public record CreateBeerDto(
    [Required, MaxLength(200)] string Name,
    [MaxLength(2000)] string Description,
    [Range(0.1, 100.0)] decimal AlcoholContent,
    [Range(0.01, 99999.99)] decimal Price,
    BeerStyle Style = BeerStyle.Other);

public record UpdateBeerDto(
    [Required, MaxLength(200)] string Name,
    [MaxLength(2000)] string Description,
    [Range(0.1, 100.0)] decimal AlcoholContent,
    [Range(0.01, 99999.99)] decimal Price,
    BeerStyle Style = BeerStyle.Other);

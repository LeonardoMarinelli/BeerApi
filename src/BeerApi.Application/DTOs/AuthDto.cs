using System.ComponentModel.DataAnnotations;

namespace BeerApi.Application.DTOs;

public record RegisterBrewerDto(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, MaxLength(200)] string BreweryName,
    [Required, MaxLength(100)] string BreweryCountry,
    [MaxLength(2000)] string BreweryDescription);

public record RegisterWholesalerDto(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, MaxLength(200)] string WholesalerName,
    [Required, MaxLength(500)] string WholesalerAddress);

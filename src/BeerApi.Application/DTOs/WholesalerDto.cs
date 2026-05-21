namespace BeerApi.Application.DTOs;

public record WholesalerDto(int Id, string Name, string Address);

public record WholesalerBeerDto(
    int BeerId,
    string BeerName,
    string BreweryName,
    decimal Price,
    int Stock);

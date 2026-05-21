using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IBeerService
{
    Task<IEnumerable<BeerDto>> GetByBreweryIdAsync(int breweryId);
    Task<BeerDto> GetByIdAsync(int id);
    Task<BeerDto> CreateAsync(int breweryId, CreateBeerDto dto);
    Task<BeerDto> UpdateAsync(int breweryId, int beerId, UpdateBeerDto dto);
    Task DeleteAsync(int breweryId, int beerId);
}

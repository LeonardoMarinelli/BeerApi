using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IBreweryService
{
    Task<IEnumerable<BreweryDto>> GetAllAsync();
    Task<BreweryDto> GetByIdAsync(int id);
}

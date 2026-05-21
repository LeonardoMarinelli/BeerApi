using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IBreweryService
{
    Task<IEnumerable<BreweryDto>> GetAllAsync(CancellationToken ct = default);
    Task<BreweryDto> GetByIdAsync(int id, CancellationToken ct = default);
}

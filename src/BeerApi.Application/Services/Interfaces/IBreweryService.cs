using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IBreweryService
{
    Task<PagedResultDto<BreweryDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<BreweryDto> GetByIdAsync(int id, CancellationToken ct = default);
}

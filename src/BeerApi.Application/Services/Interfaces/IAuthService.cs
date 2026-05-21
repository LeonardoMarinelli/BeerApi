using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IAuthService
{
    Task RegisterBrewerAsync(RegisterBrewerDto dto, CancellationToken ct = default);
    Task RegisterWholesalerAsync(RegisterWholesalerDto dto, CancellationToken ct = default);
}

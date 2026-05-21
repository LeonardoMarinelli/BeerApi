using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IAuthService
{
    Task RegisterBrewerAsync(RegisterBrewerDto dto);
    Task RegisterWholesalerAsync(RegisterWholesalerDto dto);
}

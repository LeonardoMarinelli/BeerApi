using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto);
}

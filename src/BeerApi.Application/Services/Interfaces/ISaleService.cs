using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, CancellationToken ct = default);
    Task<PagedResultDto<SaleDto>> GetAllAsync(int page, int pageSize, int? breweryId, CancellationToken ct = default);
}

using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IWholesalerService
{
    Task<IEnumerable<WholesalerDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<WholesalerBeerDto>> GetStockByWholesalerIdAsync(int wholesalerId, CancellationToken ct = default);
    Task<QuoteResponseDto> GetQuoteAsync(int wholesalerId, QuoteRequestDto request, CancellationToken ct = default);
}

using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IWholesalerService
{
    Task<IEnumerable<WholesalerDto>> GetAllAsync();
    Task<IEnumerable<WholesalerBeerDto>> GetStockByWholesalerIdAsync(int wholesalerId);
    Task<QuoteResponseDto> GetQuoteAsync(int wholesalerId, QuoteRequestDto request);
}

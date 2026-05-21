using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class SaleService(
    IBeerRepository beerRepository,
    IWholesalerRepository wholesalerRepository,
    ISaleRepository saleRepository) : ISaleService
{
    private readonly IBeerRepository _beerRepository = beerRepository;
    private readonly IWholesalerRepository _wholesalerRepository = wholesalerRepository;
    private readonly ISaleRepository _saleRepository = saleRepository;

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto)
    {
        var beer = await _beerRepository.GetByIdWithBreweryAsync(dto.BeerId)
            ?? throw new NotFoundException("Cerveja", dto.BeerId);

        var wholesaler = await _wholesalerRepository.GetByIdAsync(dto.WholesalerId)
            ?? throw new NotFoundException("Atacadista", dto.WholesalerId);

        if (dto.Quantity <= 0)
            throw new BusinessException("A quantidade da venda deve ser maior que zero.");

        var stockEntry = await _wholesalerRepository.GetStockEntryAsync(dto.WholesalerId, dto.BeerId);
        if (stockEntry is null)
        {
            stockEntry = new WholesalerBeer
            {
                WholesalerId = dto.WholesalerId,
                BeerId = dto.BeerId,
                Quantity = dto.Quantity
            };
            await _wholesalerRepository.AddStockEntryAsync(stockEntry);
        }
        else
        {
            stockEntry.Quantity += dto.Quantity;
            await _wholesalerRepository.UpdateStockEntryAsync(stockEntry);
        }

        var sale = new Sale
        {
            BreweryId = beer.BreweryId,
            WholesalerId = dto.WholesalerId,
            BeerId = dto.BeerId,
            Quantity = dto.Quantity,
            PricePerUnit = beer.Price,
            TotalPrice = dto.Quantity * beer.Price,
            TaxRate = 0m,
            SaleDate = DateTime.UtcNow
        };

        await _saleRepository.AddAsync(sale);

        return new SaleDto(
            sale.Id, beer.Id, beer.Name,
            wholesaler.Id, wholesaler.Name,
            dto.Quantity, beer.Price, sale.TotalPrice, sale.TaxRate, sale.SaleDate);
    }
}

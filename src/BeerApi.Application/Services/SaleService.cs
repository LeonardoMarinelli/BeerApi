using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class SaleService(
    IBeerRepository beerRepository,
    IWholesalerRepository wholesalerRepository,
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork) : ISaleService
{
    private readonly IBeerRepository _beerRepository = beerRepository;
    private readonly IWholesalerRepository _wholesalerRepository = wholesalerRepository;
    private readonly ISaleRepository _saleRepository = saleRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, CancellationToken ct = default)
    {
        var beer = await _beerRepository.GetByIdWithBreweryAsync(dto.BeerId, ct)
            ?? throw new NotFoundException("Cerveja", dto.BeerId);

        var wholesaler = await _wholesalerRepository.GetByIdAsync(dto.WholesalerId, ct)
            ?? throw new NotFoundException("Atacadista", dto.WholesalerId);

        if (dto.Quantity <= 0)
            throw new BusinessException("A quantidade da venda deve ser maior que zero.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var stockEntry = await _wholesalerRepository.GetStockEntryAsync(dto.WholesalerId, dto.BeerId, ct);
            if (stockEntry is null)
            {
                stockEntry = new WholesalerBeer
                {
                    WholesalerId = dto.WholesalerId,
                    BeerId = dto.BeerId,
                    Quantity = dto.Quantity
                };
                await _wholesalerRepository.AddStockEntryAsync(stockEntry, ct);
            }
            else
            {
                stockEntry.Quantity += dto.Quantity;
                await _wholesalerRepository.UpdateStockEntryAsync(stockEntry, ct);
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

            await _saleRepository.AddAsync(sale, ct);
            await _unitOfWork.CommitAsync(ct);

            return new SaleDto(
                sale.Id, beer.Id, beer.Name,
                wholesaler.Id, wholesaler.Name,
                dto.Quantity, beer.Price, sale.TotalPrice, sale.TaxRate, sale.SaleDate);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}

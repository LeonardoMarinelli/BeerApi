using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class WholesalerService(IWholesalerRepository wholesalerRepository) : IWholesalerService
{
    private readonly IWholesalerRepository _wholesalerRepository = wholesalerRepository;

    public async Task<IEnumerable<WholesalerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var wholesalers = await _wholesalerRepository.GetAllAsync(ct);
        return wholesalers.Select(w => new WholesalerDto(w.Id, w.Name, w.Address));
    }

    public async Task<IEnumerable<WholesalerBeerDto>> GetStockByWholesalerIdAsync(int wholesalerId, CancellationToken ct = default)
    {
        var wholesaler = await _wholesalerRepository.GetByIdWithStockAsync(wholesalerId, ct)
            ?? throw new NotFoundException("Atacadista", wholesalerId);

        return wholesaler.WholesalerBeers.Select(wb =>
            new WholesalerBeerDto(
                wb.BeerId,
                wb.Beer.Name,
                wb.Beer.Brewery.Name,
                wb.Beer.Price,
                wb.Quantity));
    }

    public async Task<QuoteResponseDto> GetQuoteAsync(int wholesalerId, QuoteRequestDto request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new BusinessException("O pedido não pode estar vazio.");

        var wholesaler = await _wholesalerRepository.GetByIdWithStockAsync(wholesalerId, ct)
            ?? throw new BusinessException($"Atacadista com id '{wholesalerId}' não existe.");

        var beerIds = request.Items.Select(i => i.BeerId).ToList();
        if (beerIds.Count != beerIds.Distinct().Count())
            throw new BusinessException("O pedido não pode conter cervejas duplicadas.");

        var responseItems = new List<QuoteItemResponseDto>();
        decimal totalBeforeDiscount = 0m;

        foreach (var item in request.Items)
        {
            var stockEntry = wholesaler.WholesalerBeers.FirstOrDefault(wb => wb.BeerId == item.BeerId);
            if (stockEntry is null)
                throw new BusinessException(
                    $"A cerveja com id '{item.BeerId}' não é vendida por este atacadista.");

            if (item.Quantity > stockEntry.Quantity)
                throw new BusinessException(
                    $"A quantidade solicitada ({item.Quantity}) para '{stockEntry.Beer.Name}' " +
                    $"excede o estoque disponível ({stockEntry.Quantity}).");

            var subtotal = item.Quantity * stockEntry.Beer.Price;
            totalBeforeDiscount += subtotal;
            responseItems.Add(new QuoteItemResponseDto(
                stockEntry.Beer.Name, item.Quantity, stockEntry.Beer.Price, subtotal));
        }

        int totalQuantity = request.Items.Sum(i => i.Quantity);
        decimal discountPercent = totalQuantity > 20 ? 20m : totalQuantity > 10 ? 10m : 0m;
        decimal discountAmount = totalBeforeDiscount * (discountPercent / 100m);

        decimal taxRate = 0m;
        decimal taxAmount = (totalBeforeDiscount - discountAmount) * (taxRate / 100m);
        decimal totalPrice = totalBeforeDiscount - discountAmount + taxAmount;

        return new QuoteResponseDto(
            wholesaler.Name,
            responseItems,
            totalBeforeDiscount,
            discountPercent,
            discountAmount,
            taxRate,
            taxAmount,
            totalPrice);
    }
}

using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public class WholesalerService(IWholesalerRepository wholesalerRepository) : IWholesalerService
{
    private readonly IWholesalerRepository _wholesalerRepository = wholesalerRepository;

    public async Task<PagedResultDto<WholesalerDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (wholesalers, totalCount) = await _wholesalerRepository.GetAllAsync(page, pageSize, ct);
        var items = wholesalers.Select(w => new WholesalerDto(w.Id, w.Name, w.Address));
        return new PagedResultDto<WholesalerDto>(items, page, pageSize, totalCount);
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
        if (request.Items is null || request.Items.Count is 0 or > 50)
            throw new BusinessException("A cotação deve conter entre 1 e 50 itens.");
        if (request.Items.Any(item => item.Quantity is < 1 or > 1_000_000))
            throw new BusinessException("A quantidade de cada item deve estar entre 1 e 1.000.000.");

        var wholesaler = await _wholesalerRepository.GetByIdWithStockAsync(wholesalerId, ct)
            ?? throw new NotFoundException("Atacadista", wholesalerId);

        var beerIds = request.Items.Select(i => i.BeerId).ToList();
        if (beerIds.Count != beerIds.Distinct().Count())
            throw new BusinessException("O pedido não pode conter cervejas duplicadas.");

        var discountPercent = VolumeDiscountPolicy.GetPercent(request.Items.Sum(item => item.Quantity));
        var responseItems = new List<QuoteItemResponseDto>();
        decimal totalBeforeDiscount = 0m;
        decimal totalAfterDiscount = 0m;

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

            var subtotal = decimal.Round(
                item.Quantity * stockEntry.Beer.Price,
                2,
                MidpointRounding.AwayFromZero);
            totalBeforeDiscount += subtotal;
            totalAfterDiscount += decimal.Round(
                subtotal * (100m - discountPercent) / 100m,
                2,
                MidpointRounding.AwayFromZero);
            responseItems.Add(new QuoteItemResponseDto(
                stockEntry.Beer.Name, item.Quantity, stockEntry.Beer.Price, subtotal));
        }

        var discountAmount = totalBeforeDiscount - totalAfterDiscount;

        decimal taxRate = 0m;
        decimal taxAmount = totalAfterDiscount * (taxRate / 100m);
        decimal totalPrice = totalAfterDiscount + taxAmount;

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

using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Events;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;

namespace BeerApi.Application.Services;

public sealed class StockService(
    IWholesalerRepository wholesalerRepository,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    int lowStockThreshold) : IStockService
{
    private readonly int _lowStockThreshold = Math.Max(1, lowStockThreshold);

    public Task RemoveAsync(int wholesalerId, int beerId, int quantity, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var stock = await wholesalerRepository.GetStockEntryAsync(wholesalerId, beerId, ct)
                ?? throw new NotFoundException("Estoque", beerId);
            if (quantity <= 0 || quantity > stock.Quantity)
                throw new BusinessException("A quantidade deve ser positiva e não pode exceder o estoque disponível.");

            var previousQuantity = stock.Quantity;
            stock.Quantity -= quantity;
            await wholesalerRepository.UpdateStockEntryAsync(stock, ct);

            if (previousQuantity > _lowStockThreshold && stock.Quantity <= _lowStockThreshold)
                await outbox.AddAsync(new StockLowEvent(wholesalerId, beerId, stock.Quantity, _lowStockThreshold), ct);
        }, ct);
}
namespace BeerApi.Application.Services.Interfaces;

public interface IStockService
{
    Task RemoveAsync(int wholesalerId, int beerId, int quantity, CancellationToken ct = default);
}
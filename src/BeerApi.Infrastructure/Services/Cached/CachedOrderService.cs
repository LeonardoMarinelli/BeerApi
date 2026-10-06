using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeerApi.Infrastructure.Services.Cached;

public sealed class CachedOrderService(OrderService service, HybridCache cache) : IOrderService
{
    public Task<OrderDto> CreateAsync(int wholesalerId, CreateOrderDto dto, CancellationToken ct = default) =>
        service.CreateAsync(wholesalerId, dto, ct);

    public Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        service.GetByIdAsync(id, ct);

    public Task<PagedResultDto<OrderDto>> GetAllAsync(OrderSearchFiltersDto filters, int page, int pageSize, CancellationToken ct = default) =>
        service.GetAllAsync(filters, page, pageSize, ct);

    public Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default) => service.ConfirmAsync(id, ct);
    public Task<OrderDto> ShipAsync(int id, CancellationToken ct = default) => service.ShipAsync(id, ct);
    public Task<OrderDto> CancelAsync(int id, string? reason, CancellationToken ct = default) => service.CancelAsync(id, reason, ct);

    public async Task<OrderDto> DeliverAsync(int id, CancellationToken ct = default)
    {
        var order = await service.DeliverAsync(id, ct);
        await cache.RemoveByTagAsync(CacheTags.WholesalerStockFor(order.WholesalerId), ct);
        return order;
    }
}
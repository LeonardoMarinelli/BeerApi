using BeerApi.Application.DTOs;

namespace BeerApi.Application.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(int wholesalerId, CreateOrderDto dto, CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResultDto<OrderDto>> GetAllAsync(OrderSearchFiltersDto filters, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default);
    Task<OrderDto> ShipAsync(int id, CancellationToken ct = default);
    Task<OrderDto> DeliverAsync(int id, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(int id, string? reason, CancellationToken ct = default);
}
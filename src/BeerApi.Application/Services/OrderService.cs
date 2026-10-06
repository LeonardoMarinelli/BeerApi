using BeerApi.Application.DTOs;
using BeerApi.Application.Services.Interfaces;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Events;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using BeerApi.Domain.Queries;

namespace BeerApi.Application.Services;

public sealed class OrderService(
    IOrderRepository orderRepository,
    IBeerRepository beerRepository,
    IWholesalerRepository wholesalerRepository,
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork,
    IOutbox outbox) : IOrderService
{
    public async Task<OrderDto> CreateAsync(int wholesalerId, CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.Items is null || dto.Items.Count is 0 or > 50)
            throw new BusinessException("O pedido deve conter entre 1 e 50 itens.");
        if (dto.Items.Any(item => item.Quantity is < 1 or > 1_000_000))
            throw new BusinessException("A quantidade de cada item deve estar entre 1 e 1.000.000.");
        if (dto.Items.Select(item => item.BeerId).Distinct().Count() != dto.Items.Count)
            throw new BusinessException("O pedido não pode conter cervejas duplicadas.");
        var wholesaler = await wholesalerRepository.GetByIdAsync(wholesalerId, ct)
            ?? throw new NotFoundException("Atacadista", wholesalerId);

        var beerIds = dto.Items.Select(item => item.BeerId).ToArray();
        var beers = await beerRepository.GetByIdsWithBreweryAsync(beerIds, ct);
        if (beers.Count != beerIds.Length)
        {
            var foundIds = beers.Select(beer => beer.Id).ToHashSet();
            throw new NotFoundException("Cerveja", beerIds.First(id => !foundIds.Contains(id)));
        }

        var breweryIds = beers.Select(beer => beer.BreweryId).Distinct().ToArray();
        if (breweryIds.Length != 1)
            throw new BusinessException("Todos os itens do pedido devem pertencer à mesma cervejaria.");

        var beersById = beers.ToDictionary(beer => beer.Id);
        var totalQuantity = dto.Items.Sum(item => item.Quantity);
        var discountPercent = VolumeDiscountPolicy.GetPercent(totalQuantity);
        var orderItems = dto.Items.Select(item =>
        {
            var beer = beersById[item.BeerId];
            var subtotal = decimal.Round(beer.Price * item.Quantity, 2, MidpointRounding.AwayFromZero);
            var discountedSubtotal = decimal.Round(
                subtotal * (100m - discountPercent) / 100m,
                2,
                MidpointRounding.AwayFromZero);

            return new OrderItem
            {
                BeerId = beer.Id,
                BeerName = beer.Name,
                Quantity = item.Quantity,
                UnitPrice = beer.Price,
                Subtotal = subtotal,
                DiscountedSubtotal = discountedSubtotal
            };
        }).ToList();

        var totalBeforeDiscount = orderItems.Sum(item => item.Subtotal);
        var totalPrice = orderItems.Sum(item => item.DiscountedSubtotal);
        var order = new Order
        {
            BreweryId = breweryIds[0],
            Brewery = beersById[beerIds[0]].Brewery,
            WholesalerId = wholesalerId,
            Wholesaler = wholesaler,
            TotalBeforeDiscount = totalBeforeDiscount,
            DiscountPercent = discountPercent,
            DiscountAmount = totalBeforeDiscount - totalPrice,
            TotalPrice = totalPrice,
            Items = orderItems
        };

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await orderRepository.AddAsync(order, ct);
            await outbox.AddAsync(new OrderPlacedEvent(order.Id), ct);
        }, ct);
        return Map(order);
    }

    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Pedido", id);
        return Map(order);
    }

    public async Task<PagedResultDto<OrderDto>> GetAllAsync(
        OrderSearchFiltersDto filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var criteria = new OrderSearchCriteria(filters.BreweryId, filters.WholesalerId, filters.Status);
        var (orders, totalCount) = await orderRepository.GetAllAsync(criteria, page, pageSize, ct);
        return new PagedResultDto<OrderDto>(orders.Select(Map), page, pageSize, totalCount);
    }

    public Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default) =>
        TransitionAsync(id, order => order.Confirm(DateTime.UtcNow), ct);

    public Task<OrderDto> ShipAsync(int id, CancellationToken ct = default) =>
        TransitionAsync(id, order => order.Ship(DateTime.UtcNow), ct);

    public async Task<OrderDto> DeliverAsync(int id, CancellationToken ct = default)
    {
        Order? order = null;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            order = await orderRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Pedido", id);
            var previousStatus = order.Status;
            order.Deliver(DateTime.UtcNow);
            await orderRepository.UpdateAsync(order, ct);

            foreach (var item in order.Items)
            {
                var stock = await wholesalerRepository.GetStockEntryAsync(order.WholesalerId, item.BeerId, ct);
                if (stock is null)
                {
                    stock = new WholesalerBeer
                    {
                        WholesalerId = order.WholesalerId,
                        BeerId = item.BeerId,
                        Quantity = item.Quantity
                    };
                    await wholesalerRepository.AddStockEntryAsync(stock, ct);
                }
                else
                {
                    stock.Quantity += item.Quantity;
                    await wholesalerRepository.UpdateStockEntryAsync(stock, ct);
                }

                await saleRepository.AddAsync(new Sale
                {
                    OrderId = order.Id,
                    BreweryId = order.BreweryId,
                    WholesalerId = order.WholesalerId,
                    BeerId = item.BeerId,
                    Quantity = item.Quantity,
                    PricePerUnit = item.UnitPrice,
                    TotalPrice = item.DiscountedSubtotal,
                    SaleDate = order.DeliveredAt!.Value,
                    TaxRate = 0m
                }, ct);
            }

            await outbox.AddAsync(new OrderStatusChangedEvent(id, previousStatus, order.Status), ct);
        }, ct);

        return Map(order!);
    }

    public Task<OrderDto> CancelAsync(int id, string? reason, CancellationToken ct = default) =>
        TransitionAsync(id, order => order.Cancel(reason, DateTime.UtcNow), ct);

    private async Task<OrderDto> TransitionAsync(int id, Action<Order> transition, CancellationToken ct)
    {
        Order? order = null;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            order = await orderRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Pedido", id);
            var previousStatus = order.Status;
            transition(order);
            await orderRepository.UpdateAsync(order, ct);
            await outbox.AddAsync(new OrderStatusChangedEvent(id, previousStatus, order.Status), ct);
        }, ct);
        return Map(order!);
    }

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.BreweryId,
        order.Brewery.Name,
        order.WholesalerId,
        order.Wholesaler.Name,
        order.Status,
        order.CreatedAt,
        order.ConfirmedAt,
        order.ShippedAt,
        order.DeliveredAt,
        order.CancelledAt,
        order.CancelReason,
        order.TotalBeforeDiscount,
        order.DiscountPercent,
        order.DiscountAmount,
        order.TotalPrice,
        order.Items.Select(item => new OrderItemDto(
            item.BeerId,
            item.BeerName,
            item.Quantity,
            item.UnitPrice,
            item.Subtotal,
            item.DiscountedSubtotal)).ToList());
}
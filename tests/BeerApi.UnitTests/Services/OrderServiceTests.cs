using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Events;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IBeerRepository _beerRepository = Substitute.For<IBeerRepository>();
    private readonly IWholesalerRepository _wholesalerRepository = Substitute.For<IWholesalerRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());
        _sut = new OrderService(_orderRepository, _beerRepository, _wholesalerRepository, _saleRepository, _unitOfWork, _outbox);
    }

    [Fact]
    public async Task CreateAsync_MultipleBeersFromSameBrewery_FreezesPriceAndAppliesVolumeDiscount()
    {
        var brewery = new Brewery { Id = 1, Name = "Duvel Moortgat" };
        var wholesaler = new Wholesaler { Id = 7, Name = "Atacadista" };
        var beers = new[]
        {
            new Beer { Id = 1, Name = "Duvel", BreweryId = 1, Brewery = brewery, Price = 5m },
            new Beer { Id = 2, Name = "Vedett", BreweryId = 1, Brewery = brewery, Price = 3m }
        };
        _wholesalerRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(wholesaler);
        _beerRepository.GetByIdsWithBreweryAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(beers);
        _orderRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Order>().Id = 22;
                return Task.CompletedTask;
            });
        var dto = new CreateOrderDto(null,
        [
            new CreateOrderItemDto(1, 6),
            new CreateOrderItemDto(2, 5)
        ]);

        var result = await _sut.CreateAsync(7, dto);

        result.Id.Should().Be(22);
        result.Status.Should().Be(OrderStatus.Pending);
        result.TotalBeforeDiscount.Should().Be(45m);
        result.DiscountPercent.Should().Be(10m);
        result.DiscountAmount.Should().Be(4.5m);
        result.TotalPrice.Should().Be(40.5m);
        result.Items.Should().ContainSingle(item => item.BeerId == 1 && item.UnitPrice == 5m);
        result.WholesalerName.Should().Be("Atacadista");
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).AddAsync(Arg.Is<OrderPlacedEvent>(domainEvent => domainEvent.OrderId == 22), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_BeersComeFromDifferentBreweries_ThrowsBusinessException()
    {
        _wholesalerRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Wholesaler { Id = 7 });
        _beerRepository.GetByIdsWithBreweryAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new Beer { Id = 1, BreweryId = 1 },
                new Beer { Id = 2, BreweryId = 2 }
            });
        var dto = new CreateOrderDto(null, [new CreateOrderItemDto(1, 1), new CreateOrderItemDto(2, 1)]);

        var act = () => _sut.CreateAsync(7, dto);

        await act.Should().ThrowAsync<BusinessException>();
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateBeerIds_ThrowsBusinessException()
    {
        var dto = new CreateOrderDto(null, [new CreateOrderItemDto(1, 1), new CreateOrderItemDto(1, 2)]);

        var act = () => _sut.CreateAsync(7, dto);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeliverAsync_UpdatesStockAndRecordsSaleForEveryItem()
    {
        var order = new Order
        {
            Id = 10,
            BreweryId = 2,
            Brewery = new Brewery { Id = 2, Name = "Chimay" },
            WholesalerId = 7,
            Wholesaler = new Wholesaler { Id = 7, Name = "Atacadista" },
            Items =
            [
                new OrderItem { BeerId = 1, BeerName = "Chimay Rouge", Quantity = 3, UnitPrice = 4m, Subtotal = 12m, DiscountedSubtotal = 12m },
                new OrderItem { BeerId = 2, BeerName = "Chimay Bleue", Quantity = 2, UnitPrice = 5m, Subtotal = 10m, DiscountedSubtotal = 10m }
            ]
        };
        order.Confirm(DateTime.UtcNow);
        order.Ship(DateTime.UtcNow);
        var existingStock = new WholesalerBeer { WholesalerId = 7, BeerId = 1, Quantity = 4 };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _wholesalerRepository.GetStockEntryAsync(7, 1, Arg.Any<CancellationToken>()).Returns(existingStock);
        _wholesalerRepository.GetStockEntryAsync(7, 2, Arg.Any<CancellationToken>()).Returns((WholesalerBeer?)null);

        var result = await _sut.DeliverAsync(10);

        result.Status.Should().Be(OrderStatus.Delivered);
        existingStock.Quantity.Should().Be(7);
        await _wholesalerRepository.Received(1).AddStockEntryAsync(
            Arg.Is<WholesalerBeer>(stock => stock.BeerId == 2 && stock.Quantity == 2), Arg.Any<CancellationToken>());
        await _wholesalerRepository.Received(1).UpdateStockEntryAsync(existingStock, Arg.Any<CancellationToken>());
        await _saleRepository.Received(2).AddAsync(
            Arg.Is<Sale>(sale => sale.OrderId == 10 && (sale.TotalPrice == 12m || sale.TotalPrice == 10m)), Arg.Any<CancellationToken>());
        await _outbox.Received(1).AddAsync(
            Arg.Is<OrderStatusChangedEvent>(domainEvent => domainEvent.CurrentStatus == OrderStatus.Delivered),
            Arg.Any<CancellationToken>());
    }
}
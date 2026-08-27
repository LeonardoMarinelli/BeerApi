using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class WholesalerServiceTests
{
    private readonly IWholesalerRepository _wholesalerRepository = Substitute.For<IWholesalerRepository>();
    private readonly WholesalerService _sut;

    public WholesalerServiceTests()
    {
        _sut = new WholesalerService(_wholesalerRepository);
    }

    private static Wholesaler BuildWholesalerWithStock(int beerId, int quantity, decimal price)
    {
        var brewery = new Brewery { Id = 1, Name = "Duvel Moortgat" };
        var beer = new Beer { Id = beerId, Name = "Duvel", Price = price, Brewery = brewery, BreweryId = 1 };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista Central" };
        wholesaler.WholesalerBeers.Add(new WholesalerBeer
        {
            WholesalerId = 1,
            BeerId = beerId,
            Quantity = quantity,
            Wholesaler = wholesaler,
            Beer = beer
        });
        return wholesaler;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedWholesalers()
    {
        var wholesalers = new List<Wholesaler> { new() { Id = 1, Name = "Atacadista", Address = "Rua A" } };
        _wholesalerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(wholesalers);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle().Which.Name.Should().Be("Atacadista");
    }

    [Fact]
    public async Task GetStockByWholesalerIdAsync_WholesalerDoesNotExist_ThrowsNotFoundException()
    {
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns((Wholesaler?)null);

        var act = () => _sut.GetStockByWholesalerIdAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetStockByWholesalerIdAsync_WholesalerExists_ReturnsMappedStock()
    {
        var wholesaler = BuildWholesalerWithStock(beerId: 1, quantity: 20, price: 5m);
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);

        var result = await _sut.GetStockByWholesalerIdAsync(1);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new WholesalerBeerDto(1, "Duvel", "Duvel Moortgat", 5m, 20));
    }

    [Fact]
    public async Task GetQuoteAsync_EmptyItems_ThrowsBusinessException()
    {
        var request = new QuoteRequestDto([]);

        var act = () => _sut.GetQuoteAsync(1, request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetQuoteAsync_WholesalerDoesNotExist_ThrowsBusinessException()
    {
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns((Wholesaler?)null);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(1, 5)]);

        var act = () => _sut.GetQuoteAsync(1, request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetQuoteAsync_DuplicateBeerIds_ThrowsBusinessException()
    {
        var wholesaler = BuildWholesalerWithStock(beerId: 1, quantity: 20, price: 5m);
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(1, 2), new QuoteItemRequestDto(1, 3)]);

        var act = () => _sut.GetQuoteAsync(1, request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetQuoteAsync_BeerNotSoldByWholesaler_ThrowsBusinessException()
    {
        var wholesaler = BuildWholesalerWithStock(beerId: 1, quantity: 20, price: 5m);
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(99, 2)]);

        var act = () => _sut.GetQuoteAsync(1, request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task GetQuoteAsync_QuantityExceedsStock_ThrowsBusinessException()
    {
        var wholesaler = BuildWholesalerWithStock(beerId: 1, quantity: 5, price: 5m);
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(1, 6)]);

        var act = () => _sut.GetQuoteAsync(1, request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(15, 10)]
    [InlineData(25, 20)]
    public async Task GetQuoteAsync_AppliesDiscountBasedOnTotalQuantity(int quantity, decimal expectedDiscountPercent)
    {
        var wholesaler = BuildWholesalerWithStock(beerId: 1, quantity: 100, price: 10m);
        _wholesalerRepository.GetByIdWithStockAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(1, quantity)]);

        var result = await _sut.GetQuoteAsync(1, request);

        result.DiscountPercent.Should().Be(expectedDiscountPercent);
        result.TotalBeforeDiscount.Should().Be(quantity * 10m);
        result.TotalPrice.Should().Be(quantity * 10m * (1 - expectedDiscountPercent / 100m));
    }
}

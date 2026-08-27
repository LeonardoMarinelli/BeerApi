using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BeerApi.UnitTests.Services;

public class SaleServiceTests
{
    private readonly IBeerRepository _beerRepository = Substitute.For<IBeerRepository>();
    private readonly IWholesalerRepository _wholesalerRepository = Substitute.For<IWholesalerRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());
        _sut = new SaleService(_beerRepository, _wholesalerRepository, _saleRepository, _unitOfWork);
    }

    [Fact]
    public async Task CreateSaleAsync_BeerDoesNotExist_ThrowsNotFoundException()
    {
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns((Beer?)null);
        var dto = new CreateSaleDto(1, 1, 10);

        var act = () => _sut.CreateSaleAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateSaleAsync_WholesalerDoesNotExist_ThrowsNotFoundException()
    {
        var beer = new Beer { Id = 1, BreweryId = 1, Price = 5m };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        _wholesalerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Wholesaler?)null);
        var dto = new CreateSaleDto(1, 1, 10);

        var act = () => _sut.CreateSaleAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateSaleAsync_QuantityIsZeroOrLess_ThrowsBusinessException()
    {
        var beer = new Beer { Id = 1, BreweryId = 1, Price = 5m };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista" };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        _wholesalerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        var dto = new CreateSaleDto(1, 1, 0);

        var act = () => _sut.CreateSaleAsync(dto);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task CreateSaleAsync_NoExistingStockEntry_CreatesStockAndCommits()
    {
        var beer = new Beer { Id = 1, BreweryId = 1, Name = "Duvel", Price = 5m };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista" };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        _wholesalerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        _wholesalerRepository.GetStockEntryAsync(1, 1, Arg.Any<CancellationToken>()).Returns((WholesalerBeer?)null);
        var dto = new CreateSaleDto(1, 1, 10);

        var result = await _sut.CreateSaleAsync(dto);

        result.TotalPrice.Should().Be(50m);
        await _wholesalerRepository.Received(1).AddStockEntryAsync(
            Arg.Is<WholesalerBeer>(e => e.Quantity == 10), Arg.Any<CancellationToken>());
        await _saleRepository.Received(1).AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSaleAsync_ExistingStockEntry_IncrementsQuantity()
    {
        var beer = new Beer { Id = 1, BreweryId = 1, Name = "Duvel", Price = 5m };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista" };
        var stockEntry = new WholesalerBeer { WholesalerId = 1, BeerId = 1, Quantity = 5 };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        _wholesalerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        _wholesalerRepository.GetStockEntryAsync(1, 1, Arg.Any<CancellationToken>()).Returns(stockEntry);
        var dto = new CreateSaleDto(1, 1, 10);

        await _sut.CreateSaleAsync(dto);

        stockEntry.Quantity.Should().Be(15);
        await _wholesalerRepository.Received(1).UpdateStockEntryAsync(stockEntry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSaleAsync_RepositoryThrows_Rethrows()
    {
        var beer = new Beer { Id = 1, BreweryId = 1, Name = "Duvel", Price = 5m };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista" };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        _wholesalerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(wholesaler);
        _wholesalerRepository.GetStockEntryAsync(1, 1, Arg.Any<CancellationToken>()).Returns((WholesalerBeer?)null);
        _saleRepository.AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db failure"));
        var dto = new CreateSaleDto(1, 1, 10);

        var act = () => _sut.CreateSaleAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_MapsSalesFromRepository()
    {
        var brewery = new Brewery { Id = 1, Name = "Duvel Moortgat" };
        var beer = new Beer { Id = 1, Name = "Duvel", Price = 5m, Brewery = brewery, BreweryId = 1 };
        var wholesaler = new Wholesaler { Id = 1, Name = "Atacadista" };
        var sales = new List<Sale>
        {
            new()
            {
                Id = 1, BeerId = 1, Beer = beer, WholesalerId = 1, Wholesaler = wholesaler,
                Quantity = 10, PricePerUnit = 5m, TotalPrice = 50m
            }
        };
        _saleRepository.GetAllAsync(1, 20, null, Arg.Any<CancellationToken>()).Returns((sales, sales.Count));

        var result = await _sut.GetAllAsync(1, 20, null);

        result.Items.Should().ContainSingle().Which.WholesalerName.Should().Be("Atacadista");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithBreweryFilter_PassesFilterToRepository()
    {
        _saleRepository.GetAllAsync(1, 20, 7, Arg.Any<CancellationToken>()).Returns((new List<Sale>(), 0));

        await _sut.GetAllAsync(1, 20, 7);

        await _saleRepository.Received(1).GetAllAsync(1, 20, 7, Arg.Any<CancellationToken>());
    }
}

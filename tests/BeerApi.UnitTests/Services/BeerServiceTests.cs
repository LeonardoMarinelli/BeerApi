using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class BeerServiceTests
{
    private readonly IBeerRepository _beerRepository = Substitute.For<IBeerRepository>();
    private readonly IBreweryRepository _breweryRepository = Substitute.For<IBreweryRepository>();
    private readonly BeerService _sut;

    public BeerServiceTests()
    {
        _sut = new BeerService(_beerRepository, _breweryRepository);
    }

    [Fact]
    public async Task GetByBreweryIdAsync_BreweryDoesNotExist_ThrowsNotFoundException()
    {
        _breweryRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.GetByBreweryIdAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByBreweryIdAsync_BreweryExists_ReturnsMappedBeers()
    {
        var brewery = new Brewery { Id = 1, Name = "Duvel Moortgat" };
        var beers = new List<Beer>
        {
            new() { Id = 1, Name = "Duvel", BreweryId = 1, Brewery = brewery, Price = 5m, AlcoholContent = 8.5m }
        };
        _breweryRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _beerRepository.GetByBreweryIdAsync(1, Arg.Any<CancellationToken>()).Returns(beers);

        var result = await _sut.GetByBreweryIdAsync(1);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new BeerDto(1, "Duvel", "", 8.5m, 5m, 1, "Duvel Moortgat"));
    }

    [Fact]
    public async Task GetByIdAsync_BeerDoesNotExist_ThrowsNotFoundException()
    {
        _beerRepository.GetByIdWithBreweryAsync(99, Arg.Any<CancellationToken>()).Returns((Beer?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_BreweryDoesNotExist_ThrowsNotFoundException()
    {
        _breweryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Brewery?)null);
        var dto = new CreateBeerDto("Tripel", "desc", 8.0m, 4.5m);

        var act = () => _sut.CreateAsync(1, dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_BreweryExists_AddsBeerAndReturnsDto()
    {
        var brewery = new Brewery { Id = 1, Name = "Chimay" };
        _breweryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(brewery);
        var dto = new CreateBeerDto("Chimay Blue", "desc", 9.0m, 6.0m);

        var result = await _sut.CreateAsync(1, dto);

        result.Name.Should().Be("Chimay Blue");
        result.BreweryName.Should().Be("Chimay");
        await _beerRepository.Received(1).AddAsync(Arg.Is<Beer>(b => b.Name == "Chimay Blue"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_BeerDoesNotExist_ThrowsNotFoundException()
    {
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns((Beer?)null);
        var dto = new UpdateBeerDto("Name", "desc", 5.0m, 3.0m);

        var act = () => _sut.UpdateAsync(1, 1, dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_BeerBelongsToAnotherBrewery_ThrowsBusinessException()
    {
        var beer = new Beer { Id = 1, BreweryId = 2 };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        var dto = new UpdateBeerDto("Name", "desc", 5.0m, 3.0m);

        var act = () => _sut.UpdateAsync(1, 1, dto);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesAndReturnsDto()
    {
        var brewery = new Brewery { Id = 1, Name = "Rochefort" };
        var beer = new Beer { Id = 1, BreweryId = 1, Brewery = brewery, Name = "Old", Price = 1m };
        _beerRepository.GetByIdWithBreweryAsync(1, Arg.Any<CancellationToken>()).Returns(beer);
        var dto = new UpdateBeerDto("Rochefort 10", "desc", 11.3m, 7.5m);

        var result = await _sut.UpdateAsync(1, 1, dto);

        result.Name.Should().Be("Rochefort 10");
        result.Price.Should().Be(7.5m);
        await _beerRepository.Received(1).UpdateAsync(beer, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_BeerDoesNotExist_ThrowsNotFoundException()
    {
        _beerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Beer?)null);

        var act = () => _sut.DeleteAsync(1, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_BeerBelongsToAnotherBrewery_ThrowsBusinessException()
    {
        var beer = new Beer { Id = 1, BreweryId = 2 };
        _beerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(beer);

        var act = () => _sut.DeleteAsync(1, 1);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task DeleteAsync_ValidRequest_DeletesBeer()
    {
        var beer = new Beer { Id = 1, BreweryId = 1 };
        _beerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(beer);

        await _sut.DeleteAsync(1, 1);

        await _beerRepository.Received(1).DeleteAsync(beer, Arg.Any<CancellationToken>());
    }
}

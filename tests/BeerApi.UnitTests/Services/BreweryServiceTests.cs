using AwesomeAssertions;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class BreweryServiceTests
{
    private readonly IBreweryRepository _breweryRepository = Substitute.For<IBreweryRepository>();
    private readonly BreweryService _sut;

    public BreweryServiceTests()
    {
        _sut = new BreweryService(_breweryRepository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedBreweries()
    {
        var breweries = new List<Brewery>
        {
            new() { Id = 1, Name = "Duvel Moortgat", Country = "Belgium", Description = "desc" }
        };
        _breweryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(breweries);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle()
            .Which.Name.Should().Be("Duvel Moortgat");
    }

    [Fact]
    public async Task GetByIdAsync_BreweryDoesNotExist_ThrowsNotFoundException()
    {
        _breweryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Brewery?)null);

        var act = () => _sut.GetByIdAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_BreweryExists_ReturnsMappedDto()
    {
        var brewery = new Brewery { Id = 1, Name = "Chimay", Country = "Belgium", Description = "desc" };
        _breweryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(brewery);

        var result = await _sut.GetByIdAsync(1);

        result.Name.Should().Be("Chimay");
        result.Country.Should().Be("Belgium");
    }
}

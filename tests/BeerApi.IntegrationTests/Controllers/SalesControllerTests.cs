using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class SalesControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<int> CreateBeerAsync(AuthenticatedBrewer brewer)
    {
        _client.UseBearerToken(brewer.AccessToken);
        var dto = new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m);
        var response = await _client.PostAsJsonAsync($"/api/breweries/{brewer.BreweryId}/beers", dto);
        response.EnsureSuccessStatusCode();
        var beer = await response.Content.ReadFromJsonAsync<BeerDto>();
        _client.ClearAuthorization();
        return beer!.Id;
    }

    [Fact]
    public async Task CreateSale_AsBeerOwnerBrewer_ReturnsCreated()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var beerId = await CreateBeerAsync(brewer);
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var dto = new CreateSaleDto(beerId, wholesaler.WholesalerId, 10);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSale_AsAnotherBrewersBeer_ReturnsForbidden()
    {
        var owner = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var beerId = await CreateBeerAsync(owner);
        var intruder = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(intruder.AccessToken);
        var dto = new CreateSaleDto(beerId, wholesaler.WholesalerId, 10);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSale_AsAdmin_ReturnsCreated()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var beerId = await CreateBeerAsync(brewer);
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var dto = new CreateSaleDto(beerId, wholesaler.WholesalerId, 5);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSale_InvalidQuantity_ReturnsBadRequest()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var beerId = await CreateBeerAsync(brewer);
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var dto = new CreateSaleDto(beerId, wholesaler.WholesalerId, 0);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSale_AsWholesalerRole_ReturnsForbidden()
    {
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(wholesaler.AccessToken);
        var dto = new CreateSaleDto(1, wholesaler.WholesalerId, 1);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSale_Unauthenticated_ReturnsUnauthorized()
    {
        var dto = new CreateSaleDto(1, 1, 1);

        var response = await _client.PostAsJsonAsync("/api/sales", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsOwnerBrewer_ReturnsOwnSalesOnly()
    {
        var owner = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        var ownerBeerId = await CreateBeerAsync(owner);
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(owner.AccessToken);
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleDto(ownerBeerId, wholesaler.WholesalerId, 5));

        var response = await _client.GetAsync("/api/sales");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<SaleDto>>();
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(s => s.BeerId == ownerBeerId);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsOk()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);

        var response = await _client.GetAsync("/api/sales");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

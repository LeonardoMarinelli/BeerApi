using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class WholesalersControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(int BeerId, int WholesalerId)> CreateStockedBeerAsync(int stockQuantity)
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var createBeerResponse = await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m));
        createBeerResponse.EnsureSuccessStatusCode();
        var beer = await createBeerResponse.Content.ReadFromJsonAsync<BeerDto>();

        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var saleResponse = await _client.PostAsJsonAsync(
            "/api/sales", new CreateSaleDto(beer!.Id, wholesaler.WholesalerId, stockQuantity));
        saleResponse.EnsureSuccessStatusCode();
        _client.ClearAuthorization();

        return (beer.Id, wholesaler.WholesalerId);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOk()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/wholesalers");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStock_WholesalerDoesNotExist_ReturnsNotFound()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/wholesalers/999999/beers");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetQuote_ValidRequest_AppliesDiscountForOrderAboveTen()
    {
        var (beerId, wholesalerId) = await CreateStockedBeerAsync(stockQuantity: 30);
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(beerId, 15)]);

        var response = await _client.PostAsJsonAsync($"/api/wholesalers/{wholesalerId}/quote", request);
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponseDto>();
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        quote!.DiscountPercent.Should().Be(10m);
    }

    [Fact]
    public async Task GetQuote_EmptyItems_ReturnsBadRequest()
    {
        var (_, wholesalerId) = await CreateStockedBeerAsync(stockQuantity: 10);
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.PostAsJsonAsync(
            $"/api/wholesalers/{wholesalerId}/quote", new QuoteRequestDto([]));
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuote_DuplicateBeerIds_ReturnsBadRequest()
    {
        var (beerId, wholesalerId) = await CreateStockedBeerAsync(stockQuantity: 10);
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var request = new QuoteRequestDto(
            [new QuoteItemRequestDto(beerId, 2), new QuoteItemRequestDto(beerId, 3)]);

        var response = await _client.PostAsJsonAsync($"/api/wholesalers/{wholesalerId}/quote", request);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuote_QuantityExceedsStock_ReturnsBadRequest()
    {
        var (beerId, wholesalerId) = await CreateStockedBeerAsync(stockQuantity: 5);
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);
        var request = new QuoteRequestDto([new QuoteItemRequestDto(beerId, 100)]);

        var response = await _client.PostAsJsonAsync($"/api/wholesalers/{wholesalerId}/quote", request);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

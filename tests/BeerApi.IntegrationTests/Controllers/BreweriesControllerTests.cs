using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class BreweriesControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/breweries");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOk()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/breweries");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_BreweryDoesNotExist_ReturnsNotFound()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/breweries/999999");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBeer_AsOwnerBrewer_ReturnsCreated()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var dto = new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m);

        var response = await _client.PostAsJsonAsync($"/api/breweries/{brewer.BreweryId}/beers", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetBeers_AfterCreatingBeer_InvalidatesCachedList()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var path = $"/api/breweries/{brewer.BreweryId}/beers";
        var initial = await _client.GetFromJsonAsync<List<BeerDto>>(path);
        var dto = new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m);

        initial.Should().BeEmpty();
        (await _client.PostAsJsonAsync(path, dto)).EnsureSuccessStatusCode();
        var beers = await _client.GetFromJsonAsync<List<BeerDto>>(path);
        _client.ClearAuthorization();

        beers.Should().ContainSingle(beer => beer.Name == "Duvel");
    }

    [Fact]
    public async Task CreateBeer_AsAnotherBrewersOwner_ReturnsForbidden()
    {
        var owner = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        var intruder = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(intruder.AccessToken);
        var dto = new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m);

        var response = await _client.PostAsJsonAsync($"/api/breweries/{owner.BreweryId}/beers", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBeer_AsAdmin_ReturnsCreated()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var dto = new CreateBeerDto("Westmalle Tripel", "Trappist ale", 9.5m, 6.0m);

        var response = await _client.PostAsJsonAsync($"/api/breweries/{brewer.BreweryId}/beers", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBeer_Unauthenticated_ReturnsUnauthorized()
    {
        var dto = new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m);

        var response = await _client.PostAsJsonAsync("/api/breweries/1/beers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

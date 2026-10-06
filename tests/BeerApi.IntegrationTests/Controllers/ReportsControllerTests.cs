using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Enums;
using BeerApi.Domain.Interfaces;
using BeerApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class ReportsControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(AuthenticatedBrewer Brewer, AuthenticatedWholesaler Wholesaler, int BeerId)> CreateSaleAsync(string beerName)
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var beerResponse = await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto(beerName, "Report test beer", 6.5m, 3.5m));
        var beer = await beerResponse.Content.ReadFromJsonAsync<BeerDto>();
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var saleResponse = await _client.PostAsJsonAsync(
            "/api/sales", new CreateSaleDto(beer!.Id, wholesaler.WholesalerId, 12));
        _client.ClearAuthorization();
        beerResponse.EnsureSuccessStatusCode();
        saleResponse.EnsureSuccessStatusCode();
        return (brewer, wholesaler, beer.Id);
    }

    [Fact]
    public async Task SalesReport_AdminReturnsJsonAndFormulaSafeCsv()
    {
        var (brewer, _, _) = await CreateSaleAsync("=SUM(1,2)");
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var salesResponse = await _client.GetAsync(
            $"/api/reports/sales?from={from}&to={to}&breweryId={brewer.BreweryId}");
        var sales = await salesResponse.Content.ReadFromJsonAsync<List<SalesReportDto>>();
        var csvResponse = await _client.GetAsync(
            $"/api/reports/top-beers?from={from}&to={to}&breweryId={brewer.BreweryId}&format=csv");
        var csv = await csvResponse.Content.ReadAsStringAsync();
        _client.ClearAuthorization();

        salesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        sales.Should().ContainSingle().Which.Quantity.Should().Be(12);
        csvResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        csv.Should().Contain("'=SUM(1,2)");
    }

    [Fact]
    public async Task SalesReport_WholesalerReturnsForbidden()
    {
        var (_, wholesaler, _) = await CreateSaleAsync("Wholesaler report beer");
        _client.UseBearerToken(wholesaler.AccessToken);

        var response = await _client.GetAsync("/api/reports/sales");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TopBeersRepository_AggregatesSalesForBrewery()
    {
        var (brewer, _, beerId) = await CreateSaleAsync("Top Beer Report");
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var from = DateTime.UtcNow.Date.AddDays(-1);
        var to = DateTime.UtcNow.Date.AddDays(1);

        var rows = await repository.GetTopBeersAsync(from, to, brewer.BreweryId, 10);

        rows.Should().ContainSingle().Which.BeerId.Should().Be(beerId);
        rows.Single().Quantity.Should().Be(12);
    }

    [Fact]
    public async Task StockReport_WholesalerSeesOnlyOwnRows()
    {
        var (_, wholesaler, _) = await CreateSaleAsync("Stock report beer");
        _client.UseBearerToken(wholesaler.AccessToken);

        var response = await _client.GetAsync("/api/reports/stock");
        var stock = await response.Content.ReadFromJsonAsync<List<StockReportDto>>();
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stock.Should().NotBeEmpty();
        stock!.Should().OnlyContain(item => item.WholesalerId == wholesaler.WholesalerId);
    }

    [Fact]
    public async Task SalesReport_InvalidDateRangeReturnsBadRequest()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/reports/sales?from=2024-01-01&to=2025-01-10");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OrdersByStatus_AdminReturnsStringStatuses()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(1, [new CreateOrderItemDto(1, 1)]));
        var response = await _client.GetAsync("/api/reports/orders-by-status");
        var rows = await response.Content.ReadFromJsonAsync<List<OrdersByStatusReportDto>>();
        _client.ClearAuthorization();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        rows!.Should().Contain(row => row.Status == OrderStatus.Pending && row.Count > 0);
    }
}
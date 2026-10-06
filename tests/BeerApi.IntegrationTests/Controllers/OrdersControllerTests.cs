using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Enums;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class OrdersControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_AsWholesaler_UsesOwnIdAndReturnsPendingOrder()
    {
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(wholesaler.AccessToken);
        var dto = new CreateOrderDto(null, [new CreateOrderItemDto(1, 2)]);

        var response = await _client.PostAsJsonAsync("/api/orders", dto);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        order!.WholesalerId.Should().Be(wholesaler.WholesalerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(2.50m);
    }

    [Fact]
    public async Task Create_WithBeersFromDifferentBreweries_ReturnsBadRequest()
    {
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(wholesaler.AccessToken);
        var dto = new CreateOrderDto(null,
        [
            new CreateOrderItemDto(1, 1),
            new CreateOrderItemDto(4, 1)
        ]);

        var response = await _client.PostAsJsonAsync("/api/orders", dto);
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deliver_PendingOrder_ReturnsConflict()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(1, [new CreateOrderItemDto(1, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        var response = await _client.PostAsync($"/api/orders/{order!.Id}/deliver", null);
        _client.ClearAuthorization();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deliver_ShippedOrder_UpdatesCachedStockAndCreatesSales()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var before = await _client.GetFromJsonAsync<List<WholesalerBeerDto>>("/api/wholesalers/1/beers");
        var originalBeerOneStock = before!.Single(item => item.BeerId == 1).Stock;
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(1,
            [
                new CreateOrderItemDto(1, 3),
                new CreateOrderItemDto(2, 2)
            ]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        var confirmResponse = await _client.PostAsync($"/api/orders/{created!.Id}/confirm", null);
        var shipResponse = await _client.PostAsync($"/api/orders/{created.Id}/ship", null);
        var deliverResponse = await _client.PostAsync($"/api/orders/{created.Id}/deliver", null);
        var delivered = await deliverResponse.Content.ReadFromJsonAsync<OrderDto>();
        var after = await _client.GetFromJsonAsync<List<WholesalerBeerDto>>("/api/wholesalers/1/beers");
        var sales = await _client.GetFromJsonAsync<PagedResultDto<SaleDto>>("/api/sales?pageSize=100");
        _client.ClearAuthorization();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deliverResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        delivered!.Status.Should().Be(OrderStatus.Delivered);
        after!.Single(item => item.BeerId == 1).Stock.Should().Be(originalBeerOneStock + 3);
        after!.Single(item => item.BeerId == 2).Stock.Should().Be(2);
        sales!.Items.Count(sale => sale.OrderId == created.Id).Should().Be(2);
    }

    [Fact]
    public async Task Deliver_ConcurrentRequests_OnlyAppliesInventoryOnce()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var before = await _client.GetFromJsonAsync<List<WholesalerBeerDto>>("/api/wholesalers/1/beers");
        var originalStock = before!.Single(item => item.BeerId == 1).Stock;
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(1, [new CreateOrderItemDto(1, 4)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        (await _client.PostAsync($"/api/orders/{order!.Id}/confirm", null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/orders/{order.Id}/ship", null)).EnsureSuccessStatusCode();

        var deliveries = await Task.WhenAll(
            _client.PostAsync($"/api/orders/{order.Id}/deliver", null),
            _client.PostAsync($"/api/orders/{order.Id}/deliver", null));
        var stockResponse = await _client.GetAsync("/api/wholesalers/1/beers");
        var after = await stockResponse.Content.ReadFromJsonAsync<List<WholesalerBeerDto>>();
        var sales = await _client.GetFromJsonAsync<PagedResultDto<SaleDto>>("/api/sales?pageSize=100");
        _client.ClearAuthorization();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        deliveries.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        deliveries.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        after!.Single(item => item.BeerId == 1).Stock.Should().Be(originalStock + 4);
        sales!.Items.Count(sale => sale.OrderId == order.Id).Should().Be(1);
    }

    [Fact]
    public async Task GetById_OtherWholesaler_ReturnsForbidden()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(1, [new CreateOrderItemDto(1, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        _client.ClearAuthorization();

        var other = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(other.AccessToken);
        var response = await _client.GetAsync($"/api/orders/{order!.Id}");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OrderEvents_SendNotificationsToBreweryAndWholesaler()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var beerResponse = await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto("Event Beer", "Belgian ale", 7.5m, 4m));
        var beer = await beerResponse.Content.ReadFromJsonAsync<BeerDto>();
        _client.ClearAuthorization();
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(wholesaler.AccessToken);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(null, [new CreateOrderItemDto(beer!.Id, 4)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        _client.ClearAuthorization();

        var placedEmail = await factory.EmailSender.WaitForSubjectAsync(
            brewer.Email, "Novo pedido", TimeSpan.FromSeconds(20));
        _client.UseBearerToken(brewer.AccessToken);
        var confirmResponse = await _client.PostAsync($"/api/orders/{order!.Id}/confirm", null);
        _client.ClearAuthorization();
        var statusEmail = await factory.EmailSender.WaitForSubjectAsync(
            wholesaler.Email, $"Pedido #{order.Id}:", TimeSpan.FromSeconds(20));

        beerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        placedEmail.Body.Should().Contain($"#{order.Id}");
        statusEmail.Subject.Should().Contain("Confirmed");
    }

    [Fact]
    public async Task StockOut_CrossingLowThreshold_NotifiesWholesaler()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var beerResponse = await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto("Low Stock Beer", "Belgian ale", 6.5m, 3m));
        var beer = await beerResponse.Content.ReadFromJsonAsync<BeerDto>();
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var saleResponse = await _client.PostAsJsonAsync(
            "/api/sales", new CreateSaleDto(beer!.Id, wholesaler.WholesalerId, 11));
        _client.ClearAuthorization();
        _client.UseBearerToken(wholesaler.AccessToken);
        var before = await _client.GetFromJsonAsync<List<WholesalerBeerDto>>(
            $"/api/wholesalers/{wholesaler.WholesalerId}/beers");
        var stockOutResponse = await _client.PostAsJsonAsync(
            $"/api/wholesalers/{wholesaler.WholesalerId}/beers/{beer.Id}/stock-out",
            new StockOutRequestDto(2));
        var after = await _client.GetFromJsonAsync<List<WholesalerBeerDto>>(
            $"/api/wholesalers/{wholesaler.WholesalerId}/beers");
        _client.ClearAuthorization();

        var email = await factory.EmailSender.WaitForSubjectAsync(
            wholesaler.Email, "Estoque baixo:", TimeSpan.FromSeconds(20));

        beerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        stockOutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        before!.Single(item => item.BeerId == beer.Id).Stock.Should().Be(11);
        after!.Single(item => item.BeerId == beer.Id).Stock.Should().Be(9);
        email.Body.Should().Contain("9 unidades");
    }

    [Fact]
    public async Task StockOut_OtherWholesaler_ReturnsForbidden()
    {
        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);
        var saleResponse = await _client.PostAsJsonAsync(
            "/api/sales", new CreateSaleDto(1, wholesaler.WholesalerId, 5));
        _client.ClearAuthorization();
        saleResponse.EnsureSuccessStatusCode();

        var other = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(other.AccessToken);
        var response = await _client.PostAsJsonAsync(
            $"/api/wholesalers/{wholesaler.WholesalerId}/beers/1/stock-out",
            new StockOutRequestDto(1));
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
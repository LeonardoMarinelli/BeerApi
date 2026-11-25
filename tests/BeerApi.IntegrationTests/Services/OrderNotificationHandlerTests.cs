using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Events;
using BeerApi.Infrastructure.Data;
using BeerApi.Infrastructure.Services;
using BeerApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace BeerApi.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollection))]
public class OrderNotificationHandlerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HandleAsync_RedeliveryDoesNotRepeatCompletedRecipient()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var beerResponse = await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5m));
        beerResponse.EnsureSuccessStatusCode();
        var beer = (await beerResponse.Content.ReadFromJsonAsync<BeerDto>())!;

        var wholesaler = await AuthHelper.RegisterAndLoginWholesalerAsync(_client, factory.EmailSender);
        _client.ClearAuthorization();

        var messageId = Guid.NewGuid();
        var domainEvent = new StockLowEvent(wholesaler.WholesalerId, beer.Id, 5, 10);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.ProcessedMessages.Add(new ProcessedMessage
            {
                MessageId = messageId,
                Consumer = "notifications"
            });
            await context.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(domainEvent);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<OrderNotificationHandler>();
            await handler.HandleAsync(messageId, nameof(StockLowEvent), payload);
        }

        factory.EmailSender.CountWithSubjectPrefix(wholesaler.Email, "Estoque baixo:").Should().Be(1);
    }
}

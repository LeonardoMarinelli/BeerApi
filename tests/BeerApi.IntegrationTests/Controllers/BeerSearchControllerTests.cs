using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.Domain.Enums;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class BeerSearchControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Search_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/beers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_WithStyleAndAbvFilters_ReturnsMatchingStringStyle()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/beers?style=Witbier&minAbv=4.5&maxAbv=5.0&sortBy=price&sortDir=desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<BeerDto>>();
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().Contain(beer => beer.Name == "Vedett Extra White");
        result.Items.Should().Contain(beer => beer.Name == "Hoegaarden");
        result.Items.Should().OnlyContain(beer => beer.Style == BeerStyle.Witbier);
    }

    [Fact]
    public async Task Search_InvalidSort_ReturnsBadRequest()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/beers?sortBy=sql");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchCursor_TraversesPagesWithoutDuplicateIds()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        string? cursor = null;
        var ids = new List<int>();

        do
        {
            var query = cursor is null
                ? "pageSize=3&sortBy=price&sortDir=asc"
                : $"pageSize=3&sortBy=price&sortDir=asc&cursor={Uri.EscapeDataString(cursor)}";
            var response = await _client.GetAsync($"/api/beers/cursor?{query}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = (await response.Content.ReadFromJsonAsync<CursorPageResultDto<BeerDto>>())!;
            ids.AddRange(page.Items.Select(beer => beer.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        _client.ClearAuthorization();
        ids.Should().NotBeEmpty();
        ids.Distinct().Should().HaveCount(ids.Count);
    }

    [Fact]
    public async Task SearchCursor_TamperedCursor_ReturnsBadRequest()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/beers/cursor?cursor=!!!!");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class AuditLogsControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsBrewer_ReturnsForbidden()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/audit-logs");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsOk()
    {
        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);

        var response = await _client.GetAsync("/api/audit-logs");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogDto>>();
        page!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterBrewer_AuditsBreweryWithGeneratedIdAndExcludesIdentityData()
    {
        var dto = new RegisterBrewerDto(
            $"brewer-{Guid.NewGuid():N}@test.local", AuthHelper.TestPassword,
            "John", "Brewer", $"Brewery {Guid.NewGuid():N}", "Belgium", "desc");
        var registration = await _client.PostAsJsonAsync("/api/auth/register/brewer", dto);
        registration.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.UseBearerToken(await AuthHelper.LoginAsAdminAsync(_client));
        var breweryResponse = await _client.GetAsync("/api/audit-logs?entityName=Brewery");
        var identityResponse = await _client.GetAsync("/api/audit-logs?entityName=ApplicationUser");
        _client.ClearAuthorization();

        breweryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var breweries = await breweryResponse.Content.ReadFromJsonAsync<PagedResultDto<AuditLogDto>>();
        breweries!.Items.Any(entry =>
            entry.Action == "Create" && int.TryParse(entry.EntityId, out var id) && id > 0).Should().BeTrue();

        identityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var identityLogs = await identityResponse.Content.ReadFromJsonAsync<PagedResultDto<AuditLogDto>>();
        identityLogs!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_FilteredByEntityName_ReturnsOnlyMatchingEntries()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        await _client.PostAsJsonAsync(
            $"/api/breweries/{brewer.BreweryId}/beers",
            new CreateBeerDto("Duvel", "Belgian golden ale", 8.5m, 5.0m));
        _client.ClearAuthorization();

        var adminToken = await AuthHelper.LoginAsAdminAsync(_client);
        _client.UseBearerToken(adminToken);

        var response = await _client.GetAsync("/api/audit-logs?entityName=Beer");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogDto>>();
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(a => a.EntityName == "Beer");
    }
}

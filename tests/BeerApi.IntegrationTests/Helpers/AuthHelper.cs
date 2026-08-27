using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Application.DTOs;

namespace BeerApi.IntegrationTests.Helpers;

public static class AuthHelper
{
    public const string TestPassword = "Test@12345!";

    public static async Task<AuthenticatedBrewer> RegisterAndLoginBrewerAsync(HttpClient client)
    {
        var email = $"brewer-{Guid.NewGuid():N}@test.local";
        var breweryName = $"Brewery {Guid.NewGuid():N}";
        var register = new RegisterBrewerDto(
            email, TestPassword, "John", "Brewer", breweryName, "Belgium", "Test brewery");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register/brewer", register);
        registerResponse.EnsureSuccessStatusCode();

        var token = await LoginAsync(client, email, TestPassword);
        client.UseBearerToken(token);
        var breweries = await client.GetFromJsonAsync<List<BreweryDto>>("/api/breweries");
        var brewery = breweries!.Single(b => b.Name == breweryName);
        client.ClearAuthorization();

        return new AuthenticatedBrewer(email, TestPassword, brewery.Id, breweryName, token);
    }

    public static async Task<AuthenticatedWholesaler> RegisterAndLoginWholesalerAsync(HttpClient client)
    {
        var email = $"wholesaler-{Guid.NewGuid():N}@test.local";
        var wholesalerName = $"Wholesaler {Guid.NewGuid():N}";
        var register = new RegisterWholesalerDto(
            email, TestPassword, "Jane", "Wholesaler", wholesalerName, "Test address");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register/wholesaler", register);
        registerResponse.EnsureSuccessStatusCode();

        var token = await LoginAsync(client, email, TestPassword);
        client.UseBearerToken(token);
        var wholesalers = await client.GetFromJsonAsync<List<WholesalerDto>>("/api/wholesalers");
        var wholesaler = wholesalers!.Single(w => w.Name == wholesalerName);
        client.ClearAuthorization();

        return new AuthenticatedWholesaler(email, TestPassword, wholesaler.Id, wholesalerName, token);
    }

    public static Task<string> LoginAsAdminAsync(HttpClient client) =>
        LoginAsync(client, CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }

    public static void UseBearerToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static void ClearAuthorization(this HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = null;

    private sealed record LoginResponse(string TokenType, string AccessToken, int ExpiresIn, string? RefreshToken);
}

public sealed record AuthenticatedBrewer(string Email, string Password, int BreweryId, string BreweryName, string AccessToken);

public sealed record AuthenticatedWholesaler(string Email, string Password, int WholesalerId, string WholesalerName, string AccessToken);

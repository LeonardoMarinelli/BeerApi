using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Application.DTOs;

namespace BeerApi.IntegrationTests.Helpers;

public static class AuthHelper
{
    public const string TestPassword = "Test@12345!";

    public static async Task<AuthenticatedBrewer> RegisterAndLoginBrewerAsync(HttpClient client, CapturingEmailSender emailSender)
    {
        var email = $"brewer-{Guid.NewGuid():N}@test.local";
        var breweryName = $"Brewery {Guid.NewGuid():N}";
        var register = new RegisterBrewerDto(
            email, TestPassword, "John", "Brewer", breweryName, "Belgium", "Test brewery");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register/brewer", register);
        registerResponse.EnsureSuccessStatusCode();

        await ConfirmEmailAsync(client, emailSender, email);
        var tokens = await LoginWithTokensAsync(client, email, TestPassword);
        client.UseBearerToken(tokens.AccessToken);
        var breweries = await client.GetFromJsonAsync<PagedResultDto<BreweryDto>>("/api/breweries");
        var brewery = breweries!.Items.Single(b => b.Name == breweryName);
        client.ClearAuthorization();

        return new AuthenticatedBrewer(email, TestPassword, brewery.Id, breweryName, tokens.AccessToken, tokens.RefreshToken);
    }

    public static async Task<AuthenticatedWholesaler> RegisterAndLoginWholesalerAsync(HttpClient client, CapturingEmailSender emailSender)
    {
        var email = $"wholesaler-{Guid.NewGuid():N}@test.local";
        var wholesalerName = $"Wholesaler {Guid.NewGuid():N}";
        var register = new RegisterWholesalerDto(
            email, TestPassword, "Jane", "Wholesaler", wholesalerName, "Test address");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register/wholesaler", register);
        registerResponse.EnsureSuccessStatusCode();

        await ConfirmEmailAsync(client, emailSender, email);
        var tokens = await LoginWithTokensAsync(client, email, TestPassword);
        client.UseBearerToken(tokens.AccessToken);
        var wholesalers = await client.GetFromJsonAsync<PagedResultDto<WholesalerDto>>("/api/wholesalers");
        var wholesaler = wholesalers!.Items.Single(w => w.Name == wholesalerName);
        client.ClearAuthorization();

        return new AuthenticatedWholesaler(email, TestPassword, wholesaler.Id, wholesalerName, tokens.AccessToken, tokens.RefreshToken);
    }

    public static Task<string> LoginAsAdminAsync(HttpClient client) =>
        LoginAsync(client, CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var tokens = await LoginWithTokensAsync(client, email, password);
        return tokens.AccessToken;
    }

    private static async Task<AuthTokenResponseDto> LoginWithTokensAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>())!;
    }

    private static async Task ConfirmEmailAsync(HttpClient client, CapturingEmailSender emailSender, string email)
    {
        var link = emailSender.GetLastBody(email).Split('\n', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        var response = await client.GetAsync(new Uri(link).PathAndQuery);
        response.EnsureSuccessStatusCode();
    }

    public static void UseBearerToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static void ClearAuthorization(this HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = null;

}

public sealed record AuthenticatedBrewer(string Email, string Password, int BreweryId, string BreweryName, string AccessToken, string RefreshToken);

public sealed record AuthenticatedWholesaler(string Email, string Password, int WholesalerId, string WholesalerName, string AccessToken, string RefreshToken);

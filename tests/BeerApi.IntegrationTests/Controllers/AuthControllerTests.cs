using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BeerApi.Application.DTOs;
using BeerApi.IntegrationTests.Helpers;

namespace BeerApi.IntegrationTests.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class AuthControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RegisterBrewer_ValidData_ReturnsOk()
    {
        var dto = new RegisterBrewerDto(
            $"brewer-{Guid.NewGuid():N}@test.local", AuthHelper.TestPassword,
            "John", "Brewer", $"Brewery {Guid.NewGuid():N}", "Belgium", "desc");

        var response = await _client.PostAsJsonAsync("/api/auth/register/brewer", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterBrewer_DuplicateEmail_ReturnsBadRequest()
    {
        var email = $"brewer-{Guid.NewGuid():N}@test.local";
        var dto = new RegisterBrewerDto(
            email, AuthHelper.TestPassword, "John", "Brewer", $"Brewery {Guid.NewGuid():N}", "Belgium", "desc");
        (await _client.PostAsJsonAsync("/api/auth/register/brewer", dto)).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/auth/register/brewer", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "nobody@test.local", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_ReturnsUnauthorized()
    {
        var email = $"brewer-{Guid.NewGuid():N}@test.local";
        var dto = new RegisterBrewerDto(
            email, AuthHelper.TestPassword, "John", "Brewer", "Test Brewery", "Belgium", "desc");
        (await _client.PostAsJsonAsync("/api/auth/register/brewer", dto)).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = AuthHelper.TestPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);

        brewer.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_ReplayRevokesTokenFamily()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequestDto(brewer.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var rotated = (await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponseDto>())!;

        var replayResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequestDto(brewer.RefreshToken));
        var rotatedResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequestDto(rotated.RefreshToken));

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        rotatedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);
        var logoutResponse = await _client.PostAsJsonAsync(
            "/api/auth/logout", new RefreshTokenRequestDto(brewer.RefreshToken));
        _client.ClearAuthorization();
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequestDto(brewer.RefreshToken));

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_RevokesRefreshTokens()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        var forgotResponse = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password", new EmailRequestDto(brewer.Email));
        forgotResponse.EnsureSuccessStatusCode();
        var resetToken = factory.EmailSender.GetLastBody(brewer.Email)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        const string newPassword = "NewTest@12345!";

        var resetResponse = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequestDto(brewer.Email, resetToken, newPassword));
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequestDto(brewer.RefreshToken));
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = brewer.Email, password = newPassword });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client, factory.EmailSender);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

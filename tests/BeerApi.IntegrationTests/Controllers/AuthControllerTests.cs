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
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);

        brewer.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ManageInfo_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/manage/info");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManageInfo_WithValidToken_ReturnsUserInfo()
    {
        var brewer = await AuthHelper.RegisterAndLoginBrewerAsync(_client);
        _client.UseBearerToken(brewer.AccessToken);

        var response = await _client.GetAsync("/api/auth/manage/info");
        _client.ClearAuthorization();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

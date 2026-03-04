using System.Net;
using System.Net.Http.Json;
using api.Tests.Fixtures;

namespace api.Tests.Integration;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "newuser", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409()
    {
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "duplicate", Password = "password123" }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "duplicate", Password = "password456" }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "loginuser", Password = "password123" }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "loginuser", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "wrongpwuser", Password = "password123" }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "wrongpwuser", Password = "wrongpassword" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "nobody", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = "refreshuser", Password = "password123" }
        );

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "refreshuser", Password = "password123" }
        );

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { RefreshToken = loginBody!.RefreshToken }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { RefreshToken = "totally-invalid-token" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record TokenResponse(string AccessToken, string RefreshToken);
}

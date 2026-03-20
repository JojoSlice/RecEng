using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using api.Tests.Fixtures;

namespace api.Tests.Integration;

public class LogoutEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogoutEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string AccessToken, string RefreshToken)> RegisterAndLoginAsync(string username)
    {
        var resp = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = username, Password = "password123" }
        );
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return (body!.AccessToken, body.RefreshToken);
    }

    private HttpClient WithAuth(string token)
    {
        var client = new HttpClient { BaseAddress = _client.BaseAddress };
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    [Fact]
    public async Task Logout_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { RefreshToken = "any-token" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ValidToken_Returns204()
    {
        var (accessToken, refreshToken) = await RegisterAndLoginAsync("logout_valid_user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { RefreshToken = refreshToken }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var (accessToken, refreshToken) = await RegisterAndLoginAsync("logout_revoke_user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await _client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = refreshToken });

        // Refresh token should no longer work
        _client.DefaultRequestHeaders.Authorization = null;
        var refreshResp = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { RefreshToken = refreshToken }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task Logout_UnknownToken_StillReturns204()
    {
        var (accessToken, _) = await RegisterAndLoginAsync("logout_unknown_user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { RefreshToken = "unknown-token" }
        );

        // Endpoint is idempotent — no error even for unknown tokens
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private record TokenResponse(string AccessToken, string RefreshToken);
}

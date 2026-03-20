using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using api.Tests.Fixtures;

namespace api.Tests.Integration;

public class UserEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAsync(string username)
    {
        var resp = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { Username = username, Password = "password123" }
        );
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body!.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        return (body.AccessToken, userId);
    }

    // ── GET /api/users/{userId} ───────────────────────────────────────────────

    [Fact]
    public async Task GetUser_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_ExistingUser_Returns200()
    {
        var (token, userId) = await RegisterAsync("getuser_existing");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/users/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(userId, body!.Id);
        Assert.Equal("getuser_existing", body.Username);
        Assert.False(body.HasProfilePicture);
    }

    [Fact]
    public async Task GetUser_NotFound_Returns404()
    {
        var (token, _) = await RegisterAsync("getuser_notfound");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/users/me ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_Authenticated_ReturnsOwnProfile()
    {
        var (token, userId) = await RegisterAsync("me_user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(userId, body!.Id);
        Assert.Equal("me_user", body.Username);
    }

    private record TokenResponse(string AccessToken, string RefreshToken);
    private record UserResponse(Guid Id, string Username, bool HasProfilePicture);
}

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using api.Data;
using api.Features.Videos;
using api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests.Integration;

public class VideoEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public VideoEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

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

    private HttpClient AuthClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> SeedReadyVideoAsync(string title, Guid uploadedBy)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var video = new Video(title, "test description", uploadedBy);
        video.SetReady("/uploads/test.mp4");
        db.Videos.Add(video);
        await db.SaveChangesAsync();
        return video.Id;
    }

    // ── GET /api/videos ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetVideos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/videos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVideos_OnlyReturnsReadyVideos()
    {
        var (_, userId) = await RegisterAsync("getvideos_seed_user");
        await SeedReadyVideoAsync("ready-video-title", userId);

        // Seed a processing video directly in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Videos.Add(new Video("processing-video-title", "desc", userId));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/videos");
        var body = await response.Content.ReadFromJsonAsync<List<VideoResponse>>();

        Assert.NotNull(body);
        Assert.DoesNotContain(body, v => v.Status == "processing");
    }

    // ── GET /api/videos/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetVideo_ExistingVideo_Returns200()
    {
        var (_, userId) = await RegisterAsync("getvideo_user");
        var videoId = await SeedReadyVideoAsync("get-single-video", userId);

        var response = await _client.GetAsync($"/api/videos/{videoId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVideo_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/videos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/users/{id}/videos ────────────────────────────────────────────

    [Fact]
    public async Task GetUserVideos_ReturnsVideos()
    {
        var (_, userId) = await RegisterAsync("getuservideos_user");
        await SeedReadyVideoAsync("user-video-1", userId);

        var response = await _client.GetAsync($"/api/users/{userId}/videos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<VideoResponse>>();
        Assert.NotEmpty(body!);
    }

    [Fact]
    public async Task GetUserVideos_NoVideos_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}/videos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<VideoResponse>>();
        Assert.Empty(body!);
    }

    // ── DELETE /api/videos/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteVideo_WithoutAuth_Returns401()
    {
        var (_, userId) = await RegisterAsync("delete_noauth_user");
        var videoId = await SeedReadyVideoAsync("delete-noauth-video", userId);

        var response = await _client.DeleteAsync($"/api/videos/{videoId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVideo_OwnVideo_Returns204()
    {
        var (token, userId) = await RegisterAsync("delete_own_user");
        var videoId = await SeedReadyVideoAsync("delete-own-video", userId);

        var response = await AuthClient(token).DeleteAsync($"/api/videos/{videoId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVideo_OtherUsersVideo_Returns403()
    {
        var (_, ownerId) = await RegisterAsync("delete_owner_user");
        var videoId = await SeedReadyVideoAsync("delete-other-video", ownerId);

        var (otherToken, _) = await RegisterAsync("delete_other_user");
        var response = await AuthClient(otherToken).DeleteAsync($"/api/videos/{videoId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVideo_NotFound_Returns404()
    {
        var (token, _) = await RegisterAsync("delete_notfound_user");

        var response = await AuthClient(token).DeleteAsync($"/api/videos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/videos/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateVideo_WithoutAuth_Returns401()
    {
        var (_, userId) = await RegisterAsync("update_noauth_user");
        var videoId = await SeedReadyVideoAsync("update-noauth-video", userId);

        var response = await _client.PutAsJsonAsync(
            $"/api/videos/{videoId}",
            new { Title = "new title", Description = "new desc", Tags = new List<string>() }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVideo_OwnVideo_Returns200()
    {
        var (token, userId) = await RegisterAsync("update_own_user");
        var videoId = await SeedReadyVideoAsync("update-own-video", userId);

        var response = await AuthClient(token).PutAsJsonAsync(
            $"/api/videos/{videoId}",
            new { Title = "updated title", Description = "updated desc", Tags = new List<string> { "tag1" } }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.Equal("updated title", body!.Title);
        Assert.Contains("tag1", body.Tags);
    }

    [Fact]
    public async Task UpdateVideo_OtherUsersVideo_Returns403()
    {
        var (_, ownerId) = await RegisterAsync("update_owner_user");
        var videoId = await SeedReadyVideoAsync("update-other-video", ownerId);

        var (otherToken, _) = await RegisterAsync("update_other_user");
        var response = await AuthClient(otherToken).PutAsJsonAsync(
            $"/api/videos/{videoId}",
            new { Title = "hacked", Description = "hacked", Tags = new List<string>() }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVideo_NotFound_Returns404()
    {
        var (token, _) = await RegisterAsync("update_notfound_user");

        var response = await AuthClient(token).PutAsJsonAsync(
            $"/api/videos/{Guid.NewGuid()}",
            new { Title = "title", Description = "desc", Tags = new List<string>() }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── records ───────────────────────────────────────────────────────────────

    private record TokenResponse(string AccessToken, string RefreshToken);
    private record VideoResponse(Guid Id, string Title, string Description, List<string> Tags, object Uploader, DateTimeOffset CreatedAt, string Status);
}

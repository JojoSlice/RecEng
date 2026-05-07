using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using api.Data;
using StackExchange.Redis;

namespace api.Features.Videos;

public static class GetVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos", Handle).RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(
        AppDbContext db,
        IConnectionMultiplexer redis,
        ClaimsPrincipal user,
        ILogger<AppDbContext> logger
    )
    {
        Guid? currentUserId = null;
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(idClaim) && Guid.TryParse(idClaim, out var parsed))
            currentUserId = parsed;

        var redisDb = redis.GetDatabase();
        var redisValue = await redisDb.StringGetAsync($"user:{currentUserId}:feed");

        if (redisValue.IsNullOrEmpty)
        {
            var results = await db
                .Videos.Include(v => v.Tags)
                .Where(v => v.Status == VideoStatus.Ready)
                .Join(
                    db.Users,
                    v => v.UploadedBy,
                    u => u.Id,
                    (v, u) => new { Video = v, u.Username }
                )
                .Select(r => new
                {
                    r.Video,
                    r.Username,
                    LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                    IsLikedByMe = currentUserId != null
                        && db.VideoLikes.Any(l =>
                            l.VideoId == r.Video.Id && l.UserId == currentUserId.Value
                        ),
                })
                .ToListAsync();

            logger.LogInformation("GetVideos, Count: {Count}", results.Count);

            return Results.Ok(
                results.Select(r =>
                    VideoResponse.From(r.Video, r.Username, r.LikeCount, r.IsLikedByMe)
                )
            );
        }

        var feedIds = JsonSerializer.Deserialize<List<Guid>>((string)redisValue!);

        var feedResults = await db
            .Videos.Include(v => v.Tags)
            .Where(v => v.Status == VideoStatus.Ready && feedIds!.Contains(v.Id))
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .Select(r => new
            {
                r.Video,
                r.Username,
                LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                IsLikedByMe = currentUserId != null
                    && db.VideoLikes.Any(l =>
                        l.VideoId == r.Video.Id && l.UserId == currentUserId.Value
                    ),
            })
            .ToListAsync();

        logger.LogInformation(
            "GetVideos userfeed: {User}, Count: {Count}",
            currentUserId,
            feedResults.Count
        );

        var videoMap = feedResults.ToDictionary(r => r.Video.Id);
        var ordered = feedIds!.Where(id => videoMap.ContainsKey(id)).Select(id => videoMap[id]);

        return Results.Ok(
            ordered.Select(r => VideoResponse.From(r.Video, r.Username, r.LikeCount, r.IsLikedByMe))
        );
    }
}

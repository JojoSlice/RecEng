using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Videos;

public static class GetFollowVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/follow", Handle).RequireAuthorization().RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(
        AppDbContext db,
        ClaimsPrincipal user,
        ILogger<AppDbContext> logger
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid userId))
            return Results.Unauthorized();

        var results = await db
            .Videos.Where(v =>
                db.UserFollows.Any(f => f.FollowerId == userId && f.FollowedId == v.UploadedBy)
                && v.Status == VideoStatus.Ready
            )
            .OrderByDescending(v => v.CreatedAt)
            .Include(v => v.Tags)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .Select(r => new
            {
                r.Video,
                r.Username,
                LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                IsLikedByMe = db.VideoLikes.Any(l => l.VideoId == r.Video.Id && l.UserId == userId),
            })
            .ToListAsync();

        logger.LogInformation("GetFollowVideos, Count: {Count}", results.Count);

        return Results.Ok(results.Select(r => VideoResponse.From(r.Video, r.Username, r.LikeCount, r.IsLikedByMe)));
    }
}

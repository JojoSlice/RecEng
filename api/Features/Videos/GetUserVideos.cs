using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Videos;

public static class GetUserVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id}/videos", Handle).RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(Guid id, AppDbContext db, ClaimsPrincipal user, ILogger<AppDbContext> logger)
    {
        Guid? currentUserId = null;
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(idClaim) && Guid.TryParse(idClaim, out var parsed))
            currentUserId = parsed;

        var results = await db.Videos
            .Include(v => v.Tags)
            .Where(v => v.UploadedBy == id && v.Status == VideoStatus.Ready)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .Select(r => new
            {
                r.Video,
                r.Username,
                LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                IsLikedByMe = currentUserId != null && db.VideoLikes.Any(l => l.VideoId == r.Video.Id && l.UserId == currentUserId.Value),
            })
            .ToListAsync();

        logger.LogInformation("GetUserVideos UploadedBy: {Id} returned count: {count}", id, results.Count);
        return Results.Ok(results.Select(r => VideoResponse.From(r.Video, r.Username, r.LikeCount, r.IsLikedByMe)));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Videos;

public static class GetVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{id:guid}", Handle).RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(Guid id, AppDbContext db, ClaimsPrincipal user, ILogger<AppDbContext> logger)
    {
        Guid? currentUserId = null;
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(idClaim) && Guid.TryParse(idClaim, out var parsed))
            currentUserId = parsed;

        var result = await db.Videos
            .Include(v => v.Tags)
            .Where(v => v.Id == id)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .Select(r => new
            {
                r.Video,
                r.Username,
                LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                IsLikedByMe = currentUserId != null && db.VideoLikes.Any(l => l.VideoId == r.Video.Id && l.UserId == currentUserId.Value),
            })
            .FirstOrDefaultAsync();

        if (result is null)
        {
            logger.LogWarning("GetVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found" });
        }

        logger.LogInformation("GetVideo Id: {Id} returned", result.Video.Id);
        return Results.Ok(VideoResponse.From(result.Video, result.Username, result.LikeCount, result.IsLikedByMe));
    }
}

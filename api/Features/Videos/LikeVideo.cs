using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Videos;

public static class LikeVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/videos/{videoId:guid}/like", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid videoId, AppDbContext db, ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid currentUserId))
            return Results.Unauthorized();

        var targetExists = await db.Videos.AnyAsync(u => u.Id == videoId);
        if (!targetExists)
            return Results.NotFound(new { message = "User not found" });

        var alreadyLiking = await db.VideoLikes.AnyAsync(l =>
            l.UserId == currentUserId && l.VideoId == videoId
        );

        if (alreadyLiking)
            return Results.Conflict(new { message = "Already liking" });

        db.VideoLikes.Add(new VideoLike(currentUserId, videoId));
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

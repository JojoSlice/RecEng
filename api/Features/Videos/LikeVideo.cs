using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using MassTransit;
using RecEng.Contracts.Events;

namespace api.Features.Videos;

public static class LikeVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/videos/{videoId:guid}/like", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(
        Guid videoId,
        AppDbContext db,
        ClaimsPrincipal user,
        IPublishEndpoint publishEndpoint
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid currentUserId))
            return Results.Unauthorized();

        var targetExists = await db.Videos.AnyAsync(v => v.Id == videoId);
        if (!targetExists)
            return Results.NotFound(new { message = "Video not found" });

        var alreadyLiking = await db.VideoLikes.AnyAsync(l =>
            l.UserId == currentUserId && l.VideoId == videoId
        );
        if (alreadyLiking)
            return Results.Conflict(new { message = "Already liking" });

        db.VideoLikes.Add(new VideoLike(currentUserId, videoId));
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(
            new VideoLikedEvent(
                VideoId: videoId,
                UserId: currentUserId,
                OccurredAt: DateTime.UtcNow
            )
        );

        return Results.NoContent();
    }
}

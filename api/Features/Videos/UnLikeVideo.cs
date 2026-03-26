using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using MassTransit;
using RecEng.Contracts.Events;

namespace api.Features.Videos;

public static class UnLikeVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/videos/{videoId:guid}/unlike", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("write");
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

        var like = await db.VideoLikes.FirstOrDefaultAsync(l =>
            l.UserId == currentUserId && l.VideoId == videoId
        );
        if (like == null)
            return Results.Conflict(new { message = "Already unlike" });

        db.VideoLikes.Remove(like);
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(
            new VideoUnlikedEvent(
                VideoId: videoId,
                UserId: currentUserId,
                OccurredAt: DateTimeOffset.UtcNow
            )
        );

        return Results.NoContent();
    }
}

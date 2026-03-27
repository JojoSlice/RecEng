using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MassTransit;
using RecEng.Contracts.Events;

namespace api.Features.Videos;

public static class WatchVideo
{
    public record WatchRequest(int WatchSeconds);

    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/videos/{id:guid}/watch", Handle).RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        Guid id,
        WatchRequest request,
        ClaimsPrincipal user,
        IPublishEndpoint publishEndpoint
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid userId))
            return Results.Unauthorized();

        await publishEndpoint.Publish(
            new VideoWatchedEvent(
                VideoId: id,
                UserId: userId,
                WatchSeconds: request.WatchSeconds,
                OccurredAt: DateTimeOffset.UtcNow
            )
        );

        return Results.NoContent();
    }
}

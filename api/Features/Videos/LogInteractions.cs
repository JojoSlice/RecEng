using System.Security.Claims;
using MassTransit;
using RecEng.Contracts.Events;

namespace api.Features.Videos;

public static class LogInteraction
{
    public record WatchRequest(int WatchSeconds);

    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/videos/{id:guid}/watch", Handle).RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        Guid id,
        WatchRequest request,
        ClaimsPrincipal user,
        IPublishEndpoint publishEndpoint
    )
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await publishEndpoint.Publish(
            new VideoWatchedEvent(
                VideoId: id,
                UserId: userId,
                WatchSeconds: request.WatchSeconds,
                OccurredAt: DateTime.UtcNow
            )
        );

        return Results.NoContent();
    }
}

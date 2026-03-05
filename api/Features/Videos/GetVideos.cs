using api.Data;

namespace api.Features.Videos;

public static class GetVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/", Handle);
    }

    public static async Task<IResult> Handle(AppDbContext db, ILogger<AppDbContext> logger)
    {
        var videos = await db.Videos.ToListAsync();
        logger.LogInformation("Get Videos, Count: {Count}", videos.Count);

        return Results.Ok(videos);
    }

}

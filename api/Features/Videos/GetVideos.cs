using api.Data;

namespace api.Features.Videos;

public static class GetVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos", Handle);
    }

    public static async Task<IResult> Handle(AppDbContext db, ILogger<GetVideos> logger)
    {
        var videos = await db.Videos.Include(v => v.Tags).ToListAsync();
        logger.LogInformation("Get Videos, Count: {Count}", videos.Count);

        return Results.Ok(videos.Select(VideoResponse.From));
    }

}

using api.Data;
using api.Features.Videos;

namespace api.Features.Users;

public static class GetUserVideos {

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user/{id}/videos", Handle);
    }


    public static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var videos = await db.Videos.Include(v => v.Tags).Where(v => v.UploadedBy == id).ToListAsync();

        if(!videos.Any())
        {
            logger.LogWarning("GetUserVideos failed, no videos found for UploadedBy: {Id}", id);
            return Results.NotFound(new { message = "Videos not found" });
        }

        logger.LogInformation("GetUserVideos UploadedBy: {Id} returned count: {count}", id, videos.Count);
        return Results.Ok(videos.Select(VideoResponse.From));
    }
}

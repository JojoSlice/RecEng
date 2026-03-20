using api.Data;

namespace api.Features.Videos;

public static class GetVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{id}", Handle).RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var result = await db.Videos
            .Include(v => v.Tags)
            .Where(v => v.Id == id)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .FirstOrDefaultAsync();

        if (result is null)
        {
            logger.LogWarning("GetVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found" });
        }

        logger.LogInformation("GetVideo Id: {Id} returned", result.Video.Id);
        return Results.Ok(VideoResponse.From(result.Video, result.Username));
    }
}


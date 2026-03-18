using api.Data;

namespace api.Features.Videos;

public static class GetUserVideos {

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id}/videos", Handle);
    }


    public static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var results = await db.Videos
            .Include(v => v.Tags)
            .Where(v => v.UploadedBy == id)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .ToListAsync();

        if (!results.Any())
        {
            logger.LogWarning("GetUserVideos failed, no videos found for UploadedBy: {Id}", id);
            return Results.NotFound(new { message = "Videos not found" });
        }

        logger.LogInformation("GetUserVideos UploadedBy: {Id} returned count: {count}", id, results.Count);
        return Results.Ok(results.Select(r => VideoResponse.From(r.Video, r.Username)));
    }
}

using api.Data;

namespace api.Features.Videos;

public static class GetVideos
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos", Handle);
    }

    public static async Task<IResult> Handle(AppDbContext db, ILogger<AppDbContext> logger)
    {
        var results = await db.Videos
            .Include(v => v.Tags)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .ToListAsync();

        logger.LogInformation("Get Videos, Count: {Count}", results.Count);

        return Results.Ok(results.Select(r => VideoResponse.From(r.Video, r.Username)));
    }

}

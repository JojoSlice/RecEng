using api.Data;

namespace api.Features.Videos;

public static class StreamVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{id}/stream", Handle).RequireRateLimiting("stream");
    }

    public static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var video = await db.Videos.FindAsync(id);

        if (video is null)
        {
            logger.LogWarning("StreamVideo failed, video with id {Id} not found", id);
            return Results.NotFound();
        }

        if (!File.Exists(video.FilePath))
        {
            logger.LogError("StreamVideo failed, file not found on disk for video {Id} at path {Path}", id, video.FilePath);
            return Results.NotFound();
        }

        var contentType = Path.GetExtension(video.FilePath).ToLower() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };

        logger.LogInformation("Streaming video {Id} from {Path}", id, video.FilePath);

        var absolutePath = Path.GetFullPath(video.FilePath);
        return TypedResults.PhysicalFile(absolutePath, contentType: contentType, enableRangeProcessing: true);
    }
}


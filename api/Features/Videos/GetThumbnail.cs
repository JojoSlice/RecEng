using System.Diagnostics;
using api.Data;

namespace api.Features.Videos;

public static class GetThumbnail
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{id}/thumbnail", Handle).RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid id, AppDbContext db, ILogger<AppDbContext> logger)
    {
        var video = await db.Videos.FindAsync(id);

        if (video is null)
            return Results.NotFound();

        if (!File.Exists(video.FilePath))
            return Results.NotFound();

        var thumbnailPath = Path.ChangeExtension(video.FilePath, ".thumb.jpg");

        if (!File.Exists(thumbnailPath))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{video.FilePath}\" -vframes 1 -vf scale=320:-2 -y \"{thumbnailPath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi) ?? throw new Exception("Failed to start ffmpeg");
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    logger.LogError("Thumbnail generation failed for video {Id}: {Error}", id, error);
                    return Results.Problem("Failed to generate thumbnail");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Thumbnail generation failed for video {Id}", id);
                return Results.Problem("Failed to generate thumbnail");
            }
        }

        return TypedResults.PhysicalFile(Path.GetFullPath(thumbnailPath), "image/jpeg");
    }
}

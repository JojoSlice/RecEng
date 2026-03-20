using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using Microsoft.AspNetCore.Mvc;

namespace api.Features.Videos;

public static class UploadVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/videos/", Handle).RequireAuthorization().DisableAntiforgery();
    }

    public static async Task<IResult> Handle(
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] List<string> tags,
        IFormFile file,
        AppDbContext db,
        ClaimsPrincipal user,
        IServiceScopeFactory scopeFactory,
        ILogger<AppDbContext> logger
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var usernameClaim = user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

        if (string.IsNullOrEmpty(idClaim) || string.IsNullOrEmpty(usernameClaim))
            return Results.Unauthorized();

        if (!Guid.TryParse(idClaim, out Guid id))
            return Results.Unauthorized();

        var allowedExtensions = new[] { ".mp4", ".webm", ".mov" };
        var extension = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            logger.LogWarning("Upload failed, invalid file type {Extension} for video {Title}", extension, title);
            return Results.BadRequest("Invalid file type. Allowed: .mp4, .webm, .mov");
        }

        var exist = await db.Videos.AnyAsync(v => v.Title == title);

        if (exist)
        {
            logger.LogWarning("Upload failed, title {Title} already taken", title);
            return Results.Conflict("Title already taken");
        }

        Directory.CreateDirectory("uploads");
        var fileId = Guid.NewGuid().ToString();
        var tempPath = Path.Combine("uploads", $"{fileId}_temp{extension}");
        var outputPath = Path.Combine("uploads", $"{fileId}.mp4");

        try
        {
            await using var stream = new FileStream(tempPath, FileMode.Create);
            await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save file for video {Title}", title);
            return Results.Problem("Failed to save video file");
        }

        var tagNames = (tags ?? []).Select(t => t.ToLowerInvariant()).Distinct().ToList();
        var existingTags = await db.Tags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync();

        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
        var newTags = tagNames
            .Where(name => !existingTagNames.Contains(name))
            .Select(name => new Tag(name))
            .ToList();

        var video = new Video(title, description, id);
        video.Tags.AddRange(existingTags.Concat(newTags));
        db.Videos.Add(video);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save video {Title} to database", title);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return Results.Problem("Failed to save video");
        }

        var videoId = video.Id;
        logger.LogInformation("Video {Title} created with id {Id}, transcoding in background", title, videoId);

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var bgDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                bgLogger.LogInformation("Transcoding video {Id}", videoId);
                await TranscodeAsync(tempPath, outputPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                var record = await bgDb.Videos.FindAsync(videoId);
                if (record != null)
                {
                    record.SetReady(outputPath);
                    await bgDb.SaveChangesAsync();
                }

                bgLogger.LogInformation("Transcoding complete for video {Id}", videoId);
            }
            catch (Exception ex)
            {
                bgLogger.LogError(ex, "Transcoding failed for video {Id}", videoId);
                if (File.Exists(tempPath)) File.Delete(tempPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var record = await bgDb.Videos.FindAsync(videoId);
                if (record != null)
                {
                    record.SetFailed();
                    await bgDb.SaveChangesAsync();
                }
            }
        });

        return Results.Accepted($"/api/videos/{videoId}", new { Id = videoId, video.Title });
    }

    private static async Task TranscodeAsync(string input, string output)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{input}\" -c:v libx264 -c:a aac -movflags +faststart -y \"{output}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi) ?? throw new Exception("Failed to start ffmpeg");
        var errorOutput = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await errorOutput;
            throw new Exception($"ffmpeg exited with code {process.ExitCode}: {error}");
        }
    }
}

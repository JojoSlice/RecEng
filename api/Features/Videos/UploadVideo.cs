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

        try
        {
            logger.LogInformation("Transcoding video {Title}", title);
            await TranscodeAsync(tempPath, outputPath);
            File.Delete(tempPath);
            logger.LogInformation("Transcoding complete for video {Title}", title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transcoding failed for video {Title}", title);
            File.Delete(tempPath);
            File.Delete(outputPath);
            return Results.Problem("Failed to process video");
        }

        try
        {
            var tagNames = (tags ?? []).Select(t => t.ToLowerInvariant()).Distinct().ToList();
            var existingTags = await db.Tags
                .Where(t => tagNames.Contains(t.Name))
                .ToListAsync();

            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
            var newTags = tagNames
                .Where(name => !existingTagNames.Contains(name))
                .Select(name => new Tag(name))
                .ToList();

            var video = new Video(title, description, outputPath, id);
            video.Tags.AddRange(existingTags.Concat(newTags));
            db.Videos.Add(video);
            await db.SaveChangesAsync();

            logger.LogInformation("Video {Title} uploaded with id {Id}", video.Title, video.Id);

            return Results.Created($"/videos/{video.Id}", new { video.Id, video.Title });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save video {Title} to database, cleaning up file", title);
            File.Delete(outputPath);
            return Results.Problem("Failed to save video");
        }
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

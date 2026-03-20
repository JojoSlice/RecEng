using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Videos;

public static class DeleteUserVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/videos/{id}", Handle).RequireAuthorization().RequireRateLimiting("read");
    }

    public static async Task<IResult> Handle(
        Guid id,
        AppDbContext db,
        ClaimsPrincipal user,
        ILogger<AppDbContext> logger
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid userId))
            return Results.Unauthorized();

        var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == id);

        if (video is null)
        {
            logger.LogWarning("DeleteUserVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found" });
        }

        if (video.UploadedBy != userId)
        {
            logger.LogWarning("DeleteUserVideo failed, User {UserId} does not own Video {Id}", userId, id);
            return Results.Forbid();
        }

        db.Videos.Remove(video);
        await db.SaveChangesAsync();

        if (File.Exists(video.FilePath))
            File.Delete(video.FilePath);

        logger.LogInformation("DeleteUserVideo Video {Id} deleted by User {UserId}", id, userId);
        return Results.NoContent();
    }
}

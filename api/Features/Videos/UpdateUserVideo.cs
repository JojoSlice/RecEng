using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using Microsoft.AspNetCore.Mvc;

namespace api.Features.Videos;

public static class UpdateUserVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/videos/{id}", Handle).RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        Guid id,
        [FromBody] UpdateUserVideoRequest request,
        AppDbContext db,
        ClaimsPrincipal user,
        ILogger<AppDbContext> logger
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid userId))
            return Results.Unauthorized();

        var video = await db.Videos.Include(v => v.Tags).FirstOrDefaultAsync(v => v.Id == id);

        if (video is null)
        {
            logger.LogWarning("UpdateUserVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found" });
        }

        if (video.UploadedBy != userId)
        {
            logger.LogWarning("UpdateUserVideo failed, User {UserId} does not own Video {Id}", userId, id);
            return Results.Forbid();
        }

        var tagNames = (request.Tags ?? []).Select(t => t.ToLowerInvariant()).Distinct().ToList();
        var existingTags = await db.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
        var newTags = tagNames
            .Where(name => !existingTagNames.Contains(name))
            .Select(name => new Tag(name))
            .ToList();

        video.Update(request.Title, request.Description, existingTags.Concat(newTags).ToList());
        await db.SaveChangesAsync();

        logger.LogInformation("UpdateUserVideo Video {Id} updated by User {UserId}", id, userId);
        return Results.Ok(VideoResponse.From(video));
    }

    public record UpdateUserVideoRequest(string Title, string Description, List<string>? Tags);
}

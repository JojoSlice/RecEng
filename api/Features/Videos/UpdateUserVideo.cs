using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using Microsoft.AspNetCore.Mvc;

namespace api.Features.Videos;

public static class UpdateUserVideo
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/videos/{id}", Handle).RequireAuthorization().RequireRateLimiting("read");
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

        var result = await db
            .Videos.Include(v => v.Tags)
            .Where(v => v.Id == id)
            .Join(db.Users, v => v.UploadedBy, u => u.Id, (v, u) => new { Video = v, u.Username })
            .Select(r => new
            {
                r.Video,
                r.Username,
                LikeCount = db.VideoLikes.Count(l => l.VideoId == r.Video.Id),
                IsLikedByMe = db.VideoLikes.Any(l => l.VideoId == r.Video.Id && l.UserId == userId),
            })
            .FirstOrDefaultAsync();

        if (result is null)
        {
            logger.LogWarning("UpdateUserVideo failed, Video with Id: {Id} was not found", id);
            return Results.NotFound(new { message = "Video not found" });
        }

        if (result.Video.UploadedBy != userId)
        {
            logger.LogWarning(
                "UpdateUserVideo failed, User {UserId} does not own Video {Id}",
                userId,
                id
            );
            return Results.Forbid();
        }

        var tagNames = (request.Tags ?? []).Select(t => t.ToLowerInvariant()).Distinct().ToList();
        var existingTags = await db.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
        var newTags = tagNames
            .Where(name => !existingTagNames.Contains(name))
            .Select(name => new Tag(name))
            .ToList();

        result.Video.Update(
            request.Title,
            request.Description,
            existingTags.Concat(newTags).ToList()
        );
        await db.SaveChangesAsync();

        logger.LogInformation("UpdateUserVideo Video {Id} updated by User {UserId}", id, userId);
        return Results.Ok(
            VideoResponse.From(result.Video, result.Username, result.LikeCount, result.IsLikedByMe)
        );
    }

    public record UpdateUserVideoRequest(string Title, string Description, List<string>? Tags);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;
using Microsoft.AspNetCore.Mvc;

namespace api.Features.Users;

public static class UploadProfilePicture
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/me/profile-picture", Handle).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");
    }

    private static async Task<IResult> Handle(
        IFormFile file,
        AppDbContext db,
        ClaimsPrincipal user,
        ILogger<AppDbContext> logger
    )
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid userId))
            return Results.Unauthorized();

        var extension = Path.GetExtension(file.FileName).ToLower();

        if (!AllowedExtensions.Contains(extension))
        {
            logger.LogWarning("Profile picture upload failed, invalid file type {Extension}", extension);
            return Results.BadRequest("Invalid file type. Allowed: .jpg, .jpeg, .png, .webp");
        }

        var dbUser = await db.Users.FindAsync(userId);

        if (dbUser is null)
            return Results.Unauthorized();

        Directory.CreateDirectory(Path.Combine("uploads", "profile-pictures"));
        var filePath = Path.Combine("uploads", "profile-pictures", $"{userId}{extension}");

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save profile picture for user {UserId}", userId);
            return Results.Problem("Failed to save profile picture");
        }

        dbUser.SetProfilePicture(filePath);
        await db.SaveChangesAsync();

        logger.LogInformation("Profile picture updated for user {UserId}", userId);
        return Results.Ok();
    }
}

using api.Data;

namespace api.Features.Users;

public static class GetProfilePicture
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/profile-picture", Handle);
    }

    private static async Task<IResult> Handle(Guid id, AppDbContext db)
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
            return Results.NotFound();

        var filePath = user.ProfilePicturePath ?? Path.Combine("assets", "default.jpg");

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
            return Results.NotFound();

        var extension = Path.GetExtension(fullPath).ToLower();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return TypedResults.PhysicalFile(fullPath, contentType);
    }
}

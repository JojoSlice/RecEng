using api.Data;

namespace api.Features.Users;

public static class GetUser
{
    public record Response(Guid Id, string Username, bool HasProfilePicture);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:guid}", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(Guid userId, AppDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Results.NotFound();
        return Results.Ok(new Response(user.Id, user.Username, user.ProfilePicturePath is not null));
    }
}

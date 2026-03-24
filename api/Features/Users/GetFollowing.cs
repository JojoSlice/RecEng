using api.Data;

namespace api.Features.Users;

public static class GetFollowing
{
    public record Response(Guid Id, string Username, bool HasProfilePicture);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:guid}/following", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid userId, AppDbContext db)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return Results.NotFound(new { message = "User not found" });

        var following = await db.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => new Response(f.Followed.Id, f.Followed.Username, f.Followed.ProfilePicturePath != null))
            .ToListAsync();

        return Results.Ok(following);
    }
}

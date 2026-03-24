using api.Data;

namespace api.Features.Users;

public static class GetFollowers
{
    public record Response(Guid Id, string Username, bool HasProfilePicture);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:guid}/followers", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid userId, AppDbContext db)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return Results.NotFound(new { message = "User not found" });

        var followers = await db.UserFollows
            .Where(f => f.FollowedId == userId)
            .Select(f => new Response(f.Follower.Id, f.Follower.Username, f.Follower.ProfilePicturePath != null))
            .ToListAsync();

        return Results.Ok(followers);
    }
}

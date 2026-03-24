using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Users;

public static class FollowUser
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{userId:guid}/follow", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid userId, AppDbContext db, ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid currentUserId))
            return Results.Unauthorized();

        if (currentUserId == userId)
            return Results.BadRequest(new { message = "Cannot follow yourself" });

        var targetExists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!targetExists)
            return Results.NotFound(new { message = "User not found" });

        var alreadyFollowing = await db.UserFollows
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowedId == userId);
        if (alreadyFollowing)
            return Results.Conflict(new { message = "Already following" });

        db.UserFollows.Add(new UserFollow(currentUserId, userId));
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

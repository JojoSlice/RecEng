using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Users;

public static class UnfollowUser
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/{userId:guid}/follow", Handle)
            .RequireAuthorization()
            .RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(Guid userId, AppDbContext db, ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out Guid currentUserId))
            return Results.Unauthorized();

        var follow = await db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowedId == userId);

        if (follow is null)
            return Results.NotFound(new { message = "Not following this user" });

        db.UserFollows.Remove(follow);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

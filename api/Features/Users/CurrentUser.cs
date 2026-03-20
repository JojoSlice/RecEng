using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Data;

namespace api.Features.Users;

public static class CurrentUser
{
    public record Response(Guid Id, string Username, bool HasProfilePicture);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", Handle).RequireAuthorization().RequireRateLimiting("read");
    }

    private static async Task<IResult> Handle(ClaimsPrincipal user, AppDbContext db)
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var usernameClaim = user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

        if (string.IsNullOrEmpty(idClaim) || string.IsNullOrEmpty(usernameClaim))
            return Results.Unauthorized();

        if (!Guid.TryParse(idClaim, out Guid id))
            return Results.Unauthorized();

        var dbUser = await db.Users.FindAsync(id);

        if (dbUser is null)
            return Results.Unauthorized();

        return Results.Ok(new Response(id, usernameClaim, dbUser.ProfilePicturePath is not null));
    }
}

using api.Data;
using api.Options;
using Microsoft.Extensions.Options;

namespace api.Features.Auth;

public static class Refresh
{
    public record Request(string RefreshToken);

    public record Response(string AccessToken, string RefreshToken);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", Handle);
    }

    private static async Task<IResult> Handle(
        Request req,
        AppDbContext db,
        IOptions<JwtOptions> jwtOptions,
        HttpContext httpContext
    )
    {
        var jwt = jwtOptions.Value;
        var tokenHash = Login.HashToken(req.RefreshToken);

        var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.TokenHash == tokenHash
        );

        if (storedToken is null || !storedToken.IsActive)
            return Results.Unauthorized();

        var user = await db.Users.FindAsync(storedToken.UserId);
        if (user is null)
            return Results.Unauthorized();

        var (newRefreshToken, newRefreshTokenHash) = Login.GenerateRefreshToken();

        storedToken.Revoke(newRefreshTokenHash);

        var newRefreshTokenEntity = new RefreshToken(
            newRefreshTokenHash,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays)
        );
        db.RefreshTokens.Add(newRefreshTokenEntity);
        await db.SaveChangesAsync();

        var accessToken = Login.GenerateAccessToken(user, jwt);

        return Results.Ok(new Response(accessToken, newRefreshToken));
    }
}

using api.Data;

namespace api.Features.Auth;

public static class Logout
{
    public record Request(string RefreshToken);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Request req,
        AppDbContext db,
        ILogger<AppDbContext> logger
    )
    {
        var tokenHash = Login.HashToken(req.RefreshToken);

        var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.TokenHash == tokenHash
        );

        if (storedToken is null || !storedToken.IsActive)
            return Results.NoContent();

        storedToken.Revoke();
        await db.SaveChangesAsync();

        logger.LogInformation("Refresh token revoked on logout");

        return Results.NoContent();
    }
}

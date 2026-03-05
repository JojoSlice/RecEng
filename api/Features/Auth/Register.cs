using api.Data;
using api.Features.Users;
using api.Options;
using Microsoft.Extensions.Options;

namespace api.Features.Auth;

public static class Register
{
    public record Request(string Username, string Password);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", Handle);
    }

    public static async Task<IResult> Handle(
        Request req,
        AppDbContext db,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AppDbContext> logger
    )
    {
        var exist = await db.Users.AnyAsync(u => u.Username == req.Username);

        if (exist)
        {
            logger.LogWarning("Registration failed, username {Username} already taken", req.Username);
            return Results.Conflict("Username already taken");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = new User(req.Username, hash);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User {Username} registered with id {UserId}", user.Username, user.Id);

        var jwt = jwtOptions.Value;
        var accessToken = Login.GenerateAccessToken(user, jwt);
        var (refreshToken, refreshTokenHash) = Login.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken(
            refreshTokenHash,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays)
        );
        db.RefreshTokens.Add(refreshTokenEntity);
        await db.SaveChangesAsync();

        return Results.Created($"/users/{user.Id}", new Login.Response(accessToken, refreshToken));
    }
}

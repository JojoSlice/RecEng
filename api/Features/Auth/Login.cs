using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using api.Data;
using api.Features.Users;
using api.Options;
using Microsoft.Extensions.Options;

namespace api.Features.Auth;

public static class Login
{
    public record Request(string Username, string Password);

    public record Response(string AccessToken, string RefreshToken);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Handle);
    }

    private static async Task<IResult> Handle(
        Request req,
        AppDbContext db,
        IOptions<JwtOptions> jwtOptions,
        HttpContext httpContext
    )
    {
        var jwt = jwtOptions.Value;
        var username = req.Username.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var accessToken = GenerateAccessToken(user, jwt);
        var (refreshToken, refreshTokenHash) = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken(
            refreshTokenHash,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays)
        );
        db.RefreshTokens.Add(refreshTokenEntity);
        await db.SaveChangesAsync();

        return Results.Ok(new Response(accessToken, refreshToken));
    }

    public static string GenerateAccessToken(User user, JwtOptions jwt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(jwt.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static (string token, string tokenHash) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var tokenHash = HashToken(token);
        return (token, tokenHash);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

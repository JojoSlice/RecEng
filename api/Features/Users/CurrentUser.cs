using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace api.Features.Users;

public static class CurrentUser
{
    public record Response(int Id, string Username);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", Handle).RequireAuthorization();
    }

    private static IResult Handle(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var usernameClaim = user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

        if (string.IsNullOrEmpty(idClaim) || string.IsNullOrEmpty(usernameClaim))
            return Results.Unauthorized();

        if (!int.TryParse(idClaim, out int id))
            return Results.Unauthorized();

        return Results.Ok(new Response(id, usernameClaim));
    }
}

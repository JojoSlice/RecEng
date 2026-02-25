using api.Data;
using api.Features.Users;

namespace api.Features.Auth;

public static class Register
{
    public record Request(string Username, string Password);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", Handle);
    }

    public static async Task<IResult> Handle(Request req, AppDbContext db)
    {
        var exist = await db.Users.AnyAsync(u => u.Username == req.Username);

        if (exist)
            return Results.Conflict("Username already taken");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = new User(req.Username, hash);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Created($"/users/{user.Id}", new { user.Id, user.Username });
    }
}

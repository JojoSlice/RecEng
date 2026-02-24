namespace api.Features.Users;

public static class Register
{
    public record Request(string Username, string Password);

    public static async Task<IResult> Handle(Request req, AppDbContext db)
    {
        var exist = await db.Users.AnyAsync(u => u.Username == req.Username);

        if(exist)
            return Results.Confict("Username already taken");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = new User(req.Username, hash);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Created($"/users/{user.Id}", new { user.Id, user.Username });
    }
}

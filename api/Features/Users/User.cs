namespace api.Features.Users;

public class User
{
    private User() { }

    public User(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
}

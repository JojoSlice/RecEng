namespace api.Features.Auth;

public class RefreshToken
{
    private RefreshToken() { }

    public RefreshToken(string tokenHash, Guid userId, DateTimeOffset expiresAt)
    {
        TokenHash = tokenHash;
        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

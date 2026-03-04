using api.Features.Auth;

namespace api.Tests.Unit;

public class RefreshTokenTests
{
    [Fact]
    public void NewToken_IsActive()
    {
        var token = new RefreshToken("hash", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7));

        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void ExpiredToken_IsNotActive()
    {
        var token = new RefreshToken("hash", Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void RevokedToken_IsNotActive()
    {
        var token = new RefreshToken("hash", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7));

        token.Revoke();

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Revoke_SetsReplacedByTokenHash()
    {
        var token = new RefreshToken("hash", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7));

        token.Revoke("new-hash");

        Assert.True(token.IsRevoked);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
    }
}

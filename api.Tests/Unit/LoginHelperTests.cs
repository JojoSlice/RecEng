using System.IdentityModel.Tokens.Jwt;
using api.Features.Auth;
using api.Features.Users;
using api.Options;

namespace api.Tests.Unit;

public class LoginHelperTests
{
    [Fact]
    public void HashToken_IsDeterministic()
    {
        var token = "test-token-value";

        var hash1 = Login.HashToken(token);
        var hash2 = Login.HashToken(token);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInputs_ProduceDifferentHashes()
    {
        var hash1 = Login.HashToken("token-a");
        var hash2 = Login.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var (token1, hash1) = Login.GenerateRefreshToken();
        var (token2, hash2) = Login.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateRefreshToken_HashMatchesToken()
    {
        var (token, hash) = Login.GenerateRefreshToken();

        Assert.Equal(Login.HashToken(token), hash);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var user = new User("testuser", "hash");
        var jwt = new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "super-secret-key-that-is-at-least-32-bytes-long!",
            AccessTokenExpiryMinutes = 15,
        };

        var token = Login.GenerateAccessToken(user, jwt);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var parsed = handler.ReadJwtToken(token);
        Assert.Equal("test-issuer", parsed.Issuer);
        Assert.Contains(parsed.Audiences, a => a == "test-audience");
        Assert.Equal("testuser", parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_api.Api.Models;
using backend_api.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="TokenService"/>.
/// IConfiguration is provided via an in-memory collection so no appsettings.json
/// file is required at test time.
/// </summary>
public class TokenServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(int expiresInMinutes = 60)
    {
        var dict = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"]         = "SuperSecretKeyForTestingPurposesOnly1234567890!",
            ["JwtSettings:Issuer"]            = "QuantIQ-Test",
            ["JwtSettings:Audience"]          = "QuantIQ-Client-Test",
            ["JwtSettings:ExpiresInMinutes"]  = expiresInMinutes.ToString()
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private static TokenService BuildService(int expiresInMinutes = 60)
        => new TokenService(BuildConfig(expiresInMinutes));

    private static User MakeUser(string userId = "user-abc", string role = "User") => new()
    {
        UserId       = userId,
        Username     = "testuser",
        PasswordHash = "hash",
        Role         = role
    };

    // ── GenerateToken ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
    {
        var svc = BuildService();
        var (token, _) = svc.GenerateToken(MakeUser());

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_ValidUser_TokenContainsCorrectUserIdClaim()
    {
        var svc = BuildService();
        var user = MakeUser("user-xyz");

        var (tokenString, _) = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(tokenString);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        Assert.NotNull(subClaim);
        Assert.Equal("user-xyz", subClaim!.Value);
    }

    [Fact]
    public void GenerateToken_ValidUser_TokenContainsCorrectRoleClaim()
    {
        var svc  = BuildService();
        var user = MakeUser(role: "Admin");

        var (tokenString, _) = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(tokenString);

        // ClaimTypes.Role maps to the long URI; look for either form
        var roleClaim = jwt.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Role ||
            c.Type == "role");

        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim!.Value);
    }

    [Fact]
    public void GenerateToken_ValidUser_TokenHasExpiry()
    {
        var svc = BuildService(expiresInMinutes: 30);
        var before = DateTime.UtcNow;

        var (tokenString, expiresAt) = svc.GenerateToken(MakeUser());

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(tokenString);

        // Token must expire in the future
        Assert.True(jwt.ValidTo > before);
        // Returned expiresAt must match the token's expiry (within a few seconds)
        Assert.True(Math.Abs((jwt.ValidTo - expiresAt).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateToken_ExpiresInMinutes30_ExpiryIsApproximately30MinutesFromNow()
    {
        var svc    = BuildService(expiresInMinutes: 30);
        var before = DateTime.UtcNow;

        var (_, expiresAt) = svc.GenerateToken(MakeUser());

        var deltaMinutes = (expiresAt - before).TotalMinutes;

        Assert.True(deltaMinutes >= 29 && deltaMinutes <= 31,
            $"Expected ~30 min expiry, got {deltaMinutes:F2} minutes.");
    }

    [Fact]
    public void GenerateToken_TokenContainsJtiClaim()
    {
        var svc = BuildService();
        var (tokenString, _) = svc.GenerateToken(MakeUser());

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(tokenString);

        var jtiClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);

        Assert.NotNull(jtiClaim);
        // JTI must be a non-empty GUID
        Assert.True(Guid.TryParse(jtiClaim!.Value, out _),
            $"JTI claim '{jtiClaim.Value}' is not a valid GUID.");
    }

    [Fact]
    public void GenerateToken_TwoCallsSameUser_ProduceDifferentJtiValues()
    {
        // Each call must generate a unique JTI so tokens are not identical
        var svc  = BuildService();
        var user = MakeUser();

        var (token1, _) = svc.GenerateToken(user);
        var (token2, _) = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }

    // ── GenerateRefreshToken ──────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var svc = BuildService();
        var token = svc.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValidBase64String()
    {
        var svc   = BuildService();
        var token = svc.GenerateRefreshToken();

        // Must be parseable as Base64 (64 random bytes → 88 Base64 chars with padding)
        var bytes = Convert.FromBase64String(token);

        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_TwoCallsReturnDifferentTokens()
    {
        var svc = BuildService();

        var token1 = svc.GenerateRefreshToken();
        var token2 = svc.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_MultipleCallsAllReturnUniqueValues()
    {
        var svc    = BuildService();
        var tokens = Enumerable.Range(0, 20)
                               .Select(_ => svc.GenerateRefreshToken())
                               .ToHashSet();

        // All 20 tokens must be distinct
        Assert.Equal(20, tokens.Count);
    }
}

using System.Text;
using System.Text.Json;
using FitTrack.UI.Auth;

namespace FitTrack.Unit.Auth;

public sealed class JwtReaderTests
{
    private static string MakeToken(long? expSeconds, string? name = "Atleta")
    {
        var header = Base64Url("""{"alg":"HS256","typ":"JWT"}""");
        var payload = expSeconds is null
            ? $$"""{"sub":"user-1","name":"{{name}}","role":"athlete"}"""
            : $$"""{"sub":"user-1","name":"{{name}}","role":"athlete","exp":{{expSeconds}}}""";
        return $"{header}.{Base64Url(payload)}.signature";
    }

    private static string Base64Url(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    [Fact]
    public void ATokenInTheFuture_IsNotExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var token = MakeToken(now.AddMinutes(30).ToUnixTimeSeconds());

        Assert.False(JwtReader.IsExpired(token, now));
    }

    [Fact]
    public void ATokenInThePast_IsExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var token = MakeToken(now.AddMinutes(-5).ToUnixTimeSeconds());

        Assert.True(JwtReader.IsExpired(token, now));
    }

    [Fact]
    public void ATokenAtTheExactInstant_IsExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var token = MakeToken(now.ToUnixTimeSeconds());

        Assert.True(JwtReader.IsExpired(token, now));
    }

    [Fact]
    public void ATokenWithoutExp_IsExpired()
    {
        var token = MakeToken(null);

        Assert.True(JwtReader.IsExpired(token, DateTimeOffset.UtcNow));
        Assert.Null(JwtReader.GetExpiration(token));
    }

    [Fact]
    public void ATokenWithoutExpClaim_HasNoExpiration()
    {
        // MakeToken with null exp omits the claim entirely.
        var token = MakeToken(null);

        Assert.Null(JwtReader.GetExpiration(token));
    }

    [Fact]
    public void AMalformedToken_IsExpired()
    {
        Assert.True(JwtReader.IsExpired("not-a-jwt", DateTimeOffset.UtcNow));
        Assert.True(JwtReader.IsExpired("a.b", DateTimeOffset.UtcNow));
        Assert.Empty(JwtReader.ReadClaims("not-a-jwt"));
    }

    [Fact]
    public void ReadClaims_ReadsNameAndRole()
    {
        var token = MakeToken(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(), "Claudio");

        var claims = JwtReader.ReadClaims(token);

        Assert.Contains(claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Name && claim.Value == "Claudio");
        Assert.Contains(claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == "athlete");
    }

    [Fact]
    public void GetExpiration_RoundTripsTheExpClaim()
    {
        var now = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var token = MakeToken(now.AddHours(2).ToUnixTimeSeconds());

        Assert.Equal(now.AddHours(2), JwtReader.GetExpiration(token));
    }
}
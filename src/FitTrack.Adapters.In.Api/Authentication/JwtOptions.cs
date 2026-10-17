namespace FitTrack.Adapters.In.Api.Authentication;

/// <summary>Strongly typed view of the <c>Jwt</c> configuration section.</summary>
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "fit-track";

    public string Audience { get; set; } = "fit-track";

    /// <summary>Signing key. Development supplies a placeholder; other environments must configure it.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
}

namespace FitTrack.UI.Api;

/// <summary>Wire contract: access token returned by the API.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

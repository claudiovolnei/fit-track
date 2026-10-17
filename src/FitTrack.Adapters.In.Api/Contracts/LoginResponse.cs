namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP response carrying a freshly issued access token.</summary>
internal sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

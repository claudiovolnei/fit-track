namespace FitTrack.Integration.Contracts;

/// <summary>Access token returned by the login endpoint.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

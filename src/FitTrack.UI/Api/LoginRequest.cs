namespace FitTrack.UI.Api;

/// <summary>Wire contract: signing in.</summary>
public sealed record LoginRequest(string Email, string Password);

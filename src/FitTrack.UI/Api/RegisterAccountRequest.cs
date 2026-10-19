namespace FitTrack.UI.Api;

/// <summary>Wire contract: creating an account.</summary>
public sealed record RegisterAccountRequest(string Name, string Email, string Password);

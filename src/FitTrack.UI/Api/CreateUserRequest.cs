namespace FitTrack.UI.Api;

/// <summary>Wire contract: the administrator creates an account and hands it the profiles it takes.</summary>
public sealed record CreateUserRequest(string Name, string Email, string Password, IReadOnlyList<string> Roles);

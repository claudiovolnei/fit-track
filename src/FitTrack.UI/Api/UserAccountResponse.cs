namespace FitTrack.UI.Api;

/// <summary>Wire contract: an account as the administration screen lists it, with the profiles it holds.</summary>
public sealed record UserAccountResponse(
    Guid UserId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    bool HasAvatar = false);

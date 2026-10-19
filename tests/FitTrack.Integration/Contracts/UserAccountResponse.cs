namespace FitTrack.Integration.Contracts;

/// <summary>An account as the administration endpoint returns it after creation.</summary>
public sealed record UserAccountResponse(
    Guid UserId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    bool HasAvatar = false);

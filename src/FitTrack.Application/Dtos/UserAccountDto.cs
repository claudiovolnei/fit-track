namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of an account as the administrator sees it: who the account is and which profiles it
/// holds. The roles are Identity's, so the DTO carries their names — the screen is the one that
/// decides how to call each profile.
/// </summary>
public sealed record UserAccountDto(
    Guid UserId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles);

namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of an account as the administrator sees it: who the account is and which profiles it
/// holds. The roles are Identity's, so the DTO carries their names — the screen is the one that
/// decides how to call each profile.
/// <see cref="CanAuthorOwnWorkout"/> is the release the administrator gives an athlete to build
/// their own training plan, without a personal trainer.
/// </summary>
public sealed record UserAccountDto(
    Guid UserId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    bool HasAvatar = false,
    bool CanAuthorOwnWorkout = false);

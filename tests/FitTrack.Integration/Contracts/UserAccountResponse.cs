namespace FitTrack.Integration.Contracts;

/// <summary>An account as the administration endpoint returns it after creation.</summary>
public sealed record UserAccountResponse(
    Guid UserId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    bool HasAvatar = false)
{
    /// <summary>
    /// Whether the administrator released the account to build its own training plan, without a
    /// personal trainer.
    /// </summary>
    public bool CanAuthorOwnWorkout { get; init; }
}

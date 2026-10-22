using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// What the administrator released for an account. The release lives in the account itself, not in
/// the token: a claim would only change on the next sign-in, while a revocation has to take effect
/// at once.
/// </summary>
public interface IAccountPermissions
{
    /// <summary>
    /// Whether the account may build its own training plan, without a personal trainer. The answer
    /// comes from the database on every call, so a concession or a revocation is effective right
    /// away.
    /// </summary>
    Task<bool> CanAuthorOwnWorkoutAsync(UserId userId, CancellationToken cancellationToken);
}

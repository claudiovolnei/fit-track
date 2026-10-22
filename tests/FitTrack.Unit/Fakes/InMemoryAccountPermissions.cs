using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>
/// Account-permissions fake: it answers what the administrator released for the accounts it was
/// told about. The release lives in the account, so the fake holds it per account.
/// </summary>
internal sealed class InMemoryAccountPermissions : IAccountPermissions
{
    private readonly HashSet<Guid> _released = [];

    /// <summary>Releases the account to build its own plan, the way the administrator does.</summary>
    public void Release(UserId userId) => _released.Add(userId.Value);

    /// <summary>Takes the release back.</summary>
    public void Revoke(UserId userId) => _released.Remove(userId.Value);

    public Task<bool> CanAuthorOwnWorkoutAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_released.Contains(userId.Value));
}

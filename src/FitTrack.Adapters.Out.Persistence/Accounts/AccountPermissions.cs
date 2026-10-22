using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Accounts;

/// <summary>
/// Reads what the administrator released for an account. The release is a column of the account
/// itself, so the answer always comes from the database and never from a token that could be stale.
/// </summary>
internal sealed class AccountPermissions(FitTrackDbContext dbContext) : IAccountPermissions
{
    public async Task<bool> CanAuthorOwnWorkoutAsync(UserId userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(account => account.Id == userId.Value)
            .Select(account => account.CanAuthorOwnWorkout)
            .FirstOrDefaultAsync(cancellationToken);
}

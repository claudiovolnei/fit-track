using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the accounts a plan can be assigned to. An athlete is an account holding the
/// athlete profile; the query answers on the Identity tables plus whether a plan already exists.
/// </summary>
internal sealed class AthleteQueries(FitTrackDbContext dbContext) : IQueryAthletes
{
    public async Task<IReadOnlyList<AthleteDto>> ListAsync(CancellationToken cancellationToken) =>
        await Athletes()
            .OrderBy(user => user.Email)
            .Select(user => new AthleteDto(
                user.Id,
                user.Email!,
                dbContext.WorkoutPlans.Any(plan => plan.UserId == user.Id)))
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken) =>
        Athletes().AnyAsync(user => user.Id == userId.Value, cancellationToken);

    private IQueryable<Accounts.AppUser> Athletes() =>
        dbContext.Users
            .AsNoTracking()
            .Where(user => dbContext.UserRoles
                .Join(
                    dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Any(entry => entry.UserId == user.Id && entry.Name == Roles.User));
}

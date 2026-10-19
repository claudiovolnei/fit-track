using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the accounts a series can be assigned to. An athlete is an account holding the
/// athlete profile; the query answers on the Identity tables plus the series each one follows and the
/// days left of its term.
/// </summary>
internal sealed class AthleteQueries(FitTrackDbContext dbContext) : IQueryAthletes
{
    public async Task<IReadOnlyList<AthleteDto>> ListAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var athletes = await Athletes()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var assignments = await dbContext.WorkoutSeriesAssignments
            .AsNoTracking()
            .Join(
                dbContext.WorkoutSeries.AsNoTracking(),
                assignment => assignment.WorkoutSeriesId,
                series => series.Id,
                (assignment, series) => new
                {
                    assignment.UserId,
                    assignment.WorkoutSeriesId,
                    assignment.ExpiresAt,
                    SeriesName = series.Name,
                })
            .ToListAsync(cancellationToken);

        var byUser = assignments.ToDictionary(assignment => assignment.UserId);

        return
        [
            .. athletes.Select(athlete =>
            {
                var assigned = byUser.GetValueOrDefault(athlete.Id);

                return assigned is null
                    ? new AthleteDto(athlete.Id, athlete.Email!, null, null, 0)
                    : new AthleteDto(
                        athlete.Id,
                        athlete.Email!,
                        assigned.WorkoutSeriesId,
                        assigned.SeriesName,
                        WorkoutSeriesAssignment.DaysRemainingUntil(assigned.ExpiresAt, now));
            }),
        ];
    }

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

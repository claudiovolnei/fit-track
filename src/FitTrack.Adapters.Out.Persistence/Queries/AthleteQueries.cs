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
        CancellationToken cancellationToken) =>
        (await ListAsync(now, ListQuery.All, cancellationToken)).Items;

    public async Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var source = Athletes();

        if (query.Search is { } search)
        {
            // The read model has no display name yet, so the e-mail is what the trainer searches by.
            var pattern = SearchPattern.Contains(search);
            source = source.Where(user => EF.Functions.ILike(user.Email!, pattern, "\\"));
        }

        var total = await source.CountAsync(cancellationToken);

        var ordered = source
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id);

        var page = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var athletes = await page
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var ids = athletes.Select(athlete => athlete.Id).ToList();

        var assignments = await dbContext.WorkoutSeriesAssignments
            .AsNoTracking()
            .Where(assignment => ids.Contains(assignment.UserId))
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

        var items = athletes
            .Select(athlete =>
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
            })
            .ToList();

        return new PagedResult<AthleteDto>(items, query.Page, query.PageSize, total);
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

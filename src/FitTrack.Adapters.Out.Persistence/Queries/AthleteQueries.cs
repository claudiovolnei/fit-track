using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the accounts the roster assigns to a personal trainer. An athlete is an
/// account holding the athlete profile; the query answers on the Identity tables, on the
/// <c>trainer_athlete_assignments</c> link and on the series each one follows.
/// </summary>
internal sealed class AthleteQueries(FitTrackDbContext dbContext) : IQueryAthletes
{
    /// <summary>One projected row of the <c>AspNetUsers</c> read: the id and the e-mail the screens show.</summary>
    private sealed record AthleteRow(Guid Id, string? Email);

    /// <summary>What the athlete follows, until the days left are counted at the read instant.</summary>
    private sealed record FollowedSeries(Guid SeriesId, string Name, DateTimeOffset ExpiresAt);

    public async Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken) =>
        await ComposeAsync(Filter(Athletes(), query), query, now,
            includeFollowedSeries: true, includeTrainers: true, cancellationToken);

    public async Task<IReadOnlyList<AthleteDto>> ListAsync(
        DateTimeOffset now,
        UserId trainerId,
        CancellationToken cancellationToken) =>
        (await ListAsync(now, ListQuery.All, trainerId, cancellationToken)).Items;

    public async Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        UserId trainerId,
        CancellationToken cancellationToken) =>
        await ComposeAsync(Filter(Roster(trainerId), query), query, now,
            includeFollowedSeries: true, includeTrainers: false, cancellationToken);

    public async Task<PagedResult<AthleteDto>> ListUnlinkedAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken) =>
        await ComposeAsync(Filter(Unlinked(), query), query, now,
            includeFollowedSeries: false, includeTrainers: false, cancellationToken);

    public Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken) =>
        Athletes().AnyAsync(user => user.Id == userId.Value, cancellationToken);

    public Task<bool> ExistsPersonalTrainerAsync(UserId userId, CancellationToken cancellationToken) =>
        PersonalTrainers().AnyAsync(user => user.Id == userId.Value, cancellationToken);

    // ---- read-model helpers ------------------------------------------------------------

    /// <summary>Only the athletes linked to the trainer: the roster the personal screens read.</summary>
    private IQueryable<Accounts.AppUser> Roster(UserId trainerId) =>
        Athletes().Where(user => dbContext.TrainerAthleteAssignments
            .Where(link => link.TrainerId == trainerId.Value)
            .Select(link => link.AthleteId)
            .Contains(user.Id));

    /// <summary>Athletes still linked to nobody: the candidates the link screen offers.</summary>
    private IQueryable<Accounts.AppUser> Unlinked() =>
        Athletes().Where(user => !dbContext.TrainerAthleteAssignments
            .Select(link => link.AthleteId)
            .Contains(user.Id));

    private static IQueryable<Accounts.AppUser> Filter(IQueryable<Accounts.AppUser> source, ListQuery query)
    {
        if (query.Search is not { } search)
        {
            return source;
        }

        // The read model has no display name yet, so the e-mail is what the trainer searches by.
        var pattern = SearchPattern.Contains(search);
        return source.Where(user => EF.Functions.ILike(user.Email!, pattern, "\\"));
    }

    private async Task<PagedResult<AthleteDto>> ComposeAsync(
        IQueryable<Accounts.AppUser> source,
        ListQuery query,
        DateTimeOffset now,
        bool includeFollowedSeries,
        bool includeTrainers,
        CancellationToken cancellationToken)
    {
        var total = await source.CountAsync(cancellationToken);

        var ordered = source
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id);

        var listed = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var athletes = await listed
            .Select(user => new AthleteRow(user.Id, user.Email))
            .ToListAsync(cancellationToken);

        var ids = athletes.Select(athlete => athlete.Id).ToList();

        Dictionary<Guid, FollowedSeries> followed = includeFollowedSeries
            ? await FollowedSeriesByAthlete(ids, cancellationToken)
            : [];

        Dictionary<Guid, string> trainers = includeTrainers
            ? await TrainersByAthlete(ids, cancellationToken)
            : [];

        var items = athletes
            .Select(athlete =>
            {
                var series = followed.GetValueOrDefault(athlete.Id);

                return new AthleteDto(
                    athlete.Id,
                    athlete.Email!,
                    series?.SeriesId,
                    series?.Name,
                    series is null ? 0 : WorkoutSeriesAssignment.DaysRemainingUntil(series.ExpiresAt, now),
                    trainers.GetValueOrDefault(athlete.Id));
            })
            .ToList();

        return new PagedResult<AthleteDto>(items, query.Page, query.PageSize, total);
    }

    private async Task<Dictionary<Guid, FollowedSeries>> FollowedSeriesByAthlete(
        List<Guid> ids,
        CancellationToken cancellationToken) =>
        await dbContext.WorkoutSeriesAssignments
            .AsNoTracking()
            .Where(assignment => ids.Contains(assignment.UserId))
            .Join(
                dbContext.WorkoutSeries.AsNoTracking(),
                assignment => assignment.WorkoutSeriesId,
                series => series.Id,
                (assignment, series) => new
                {
                    assignment.UserId,
                    Series = new FollowedSeries(series.Id, series.Name, assignment.ExpiresAt),
                })
            .ToDictionaryAsync(entry => entry.UserId, entry => entry.Series, cancellationToken);

    private async Task<Dictionary<Guid, string>> TrainersByAthlete(
        List<Guid> ids,
        CancellationToken cancellationToken) =>
        await dbContext.TrainerAthleteAssignments
            .AsNoTracking()
            .Where(link => ids.Contains(link.AthleteId))
            .Join(
                dbContext.Users.AsNoTracking(),
                link => link.TrainerId,
                user => user.Id,
                (link, user) => new { link.AthleteId, TrainerEmail = user.Email! })
            .ToDictionaryAsync(entry => entry.AthleteId, entry => entry.TrainerEmail, cancellationToken);

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

    private IQueryable<Accounts.AppUser> PersonalTrainers() =>
        dbContext.Users
            .AsNoTracking()
            .Where(user => dbContext.UserRoles
                .Join(
                    dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Any(entry => entry.UserId == user.Id && entry.Name == Roles.PersonalTrainer));
}

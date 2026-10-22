using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Creates a reusable workout series. The name is unique per author, checked here so the caller gets
/// a stated rule instead of the raw unique-index error.
/// <para>
/// A personal trainer prescribes a series to one athlete or to several of their own roster at once:
/// the roster rules every destination, and each athlete keeps their own assignment with its own
/// 60-day term — the series they share is one and the same. An athlete the administrator released to
/// build their own plan creates the series for themselves and is its author — any athletes on the
/// request are ignored, and the sheet of the cycle they are in can be reused, so a plan they already
/// train can become theirs to adjust.
/// </para>
/// </summary>
public sealed class CreateWorkoutSeries : ICreateWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IQueryWorkoutSeries _query;
    private readonly IQueryAthletes _athletes;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly IAccountPermissions _permissions;

    public CreateWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryWorkoutSeries query,
        IQueryAthletes athletes,
        ITrainerAthleteAssignmentRepository links,
        IWorkoutSeriesAssignmentRepository assignments,
        IClock clock,
        IIdGenerator idGenerator,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _series = series;
        _query = query;
        _athletes = athletes;
        _links = links;
        _assignments = assignments;
        _clock = clock;
        _idGenerator = idGenerator;
        _permissions = permissions;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(
        CreateWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        // The athlete who builds their own plan is not a personal trainer: the release the
        // administrator gave them is what opens the door.
        var selfAuthored = !_currentUser.IsPersonalTrainer;

        if (selfAuthored)
        {
            await WorkoutSeriesAccess.RequireAuthorAsync(
                _currentUser,
                _permissions,
                "create a workout series",
                cancellationToken);
        }

        var name = new WorkoutSeriesName(command.Name);

        if (await _series.NameTakenAsync(name, _currentUser.Id, excluding: null, cancellationToken))
        {
            throw new DomainException(
                "workout_series.name.duplicate",
                $"A workout series named '{name.Value}' already exists.");
        }

        // The destinations are the whole prescription: where the sheet of the previous cycle comes
        // from, how many assignments follow, and whether the series still reads as a single-athlete
        // one. The caller's order is kept — with one destination nothing changes, and with several
        // the first one holding an assignment donates the previous sheet.
        var destinations = DestinationList(command, selfAuthored);

        // All-or-nothing starts at the validation: an athlete that does not exist or sits on another
        // roster fails the request before anything is saved. The self-authored athlete owns the only
        // destination there is — nobody prescribes to them, so nobody checks a roster.
        if (!selfAuthored)
        {
            foreach (var athlete in destinations)
            {
                if (!await _athletes.ExistsAsync(athlete, cancellationToken))
                {
                    throw new NotFoundException($"No athlete with id '{athlete.Value}'.");
                }

                // The roster rules what a trainer can prescribe: an unlinked athlete or one under
                // another trainer is refused the same way, without revealing whose roster the
                // athlete is on.
                var roster = await _links.GetByAthleteIdAsync(athlete, cancellationToken);

                if (roster is null || roster.TrainerId != _currentUser.Id)
                {
                    throw new ForbiddenException(
                        "A personal trainer only prescribes series for their own athletes.");
                }
            }
        }

        // The links the athletes follow, closed or not: they are where the sheets of the past cycles
        // live, one per athlete. Nothing follows nobody: a creation without destinations spares the
        // query.
        IReadOnlyList<WorkoutSeriesAssignment> existing = [];

        if (destinations.Count > 0)
        {
            existing = await _assignments.GetByUserIdsAsync(destinations, cancellationToken);
        }

        // The template keeps an athlete of its own while it prescribes to exactly one: the series of
        // a shared plan answers nothing in that field — who follows it lives on the assignments.
        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_idGenerator.NewId()),
            name,
            _currentUser.Id,
            destinations.Count == 1 ? destinations[0] : null);

        // The prescription travels with the cycle: the trainer moves the loads, not the sheet. With
        // several destinations the sheet comes from the first of them coming from a cycle, in the
        // caller's order — never the order the lookup happens to return — so a shared plan copies
        // its base once, not per person.
        var previous = command.CopyPreviousWorkouts
            ? destinations
                .Select(destination => existing.FirstOrDefault(assignment => assignment.UserId == destination))
                .FirstOrDefault(assignment => assignment is not null)
            : null;

        if (previous is not null)
        {
            // The past cycle carries the workouts the new sheet starts from — one copy for the whole
            // plan, however many athletes follow it.
            var source = await _series.GetByIdAsync(previous.SeriesId, cancellationToken);

            if (source is not null)
            {
                foreach (var workout in source.Workouts)
                {
                    series.AddWorkout(
                        new WorkoutSeriesWorkoutId(_idGenerator.NewId()),
                        workout.Name,
                        [.. workout.Exercises]);
                }
            }
        }

        await _series.SaveAsync(series, cancellationToken);

        // Each destination keeps their own link: a cycle the athlete already follows is pointed at
        // the new series (reassign, the 60-day term restarts); a fresh one is opened. The shared
        // series is one row — the assignments are what make it several people's plan.
        foreach (var athlete in destinations)
        {
            var link = existing.FirstOrDefault(assignment => assignment.UserId == athlete);
            WorkoutSeriesAssignment assignment;

            if (link is null)
            {
                assignment = WorkoutSeriesAssignment.Assign(
                    new WorkoutSeriesAssignmentId(_idGenerator.NewId()),
                    series.Id,
                    athlete,
                    _currentUser.Id,
                    _clock.UtcNow);
            }
            else
            {
                link.Reassign(series.Id, _currentUser.Id, _clock.UtcNow);
                assignment = link;
            }

            await _assignments.SaveAsync(assignment, cancellationToken);
        }

        return await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);
    }

    /// <summary>
    /// The athletes the series is born for. The self-authored case has none to choose: the athlete is
    /// the author and the destination at the same time, and the athletes of the request are ignored.
    /// <para>
    /// In the trainer case the single <c>AthleteId</c> (when present) leads the merged list, the
    /// explicit list follows, duplications keep their first occurrence and an empty id counts as
    /// none. With only the single id the list is exactly the one destination — the behaviour of the
    /// flows that came before the plural, source of the previous sheet included.
    /// </para>
    /// </summary>
    private IReadOnlyList<UserId> DestinationList(CreateWorkoutSeriesCommand command, bool selfAuthored)
    {
        if (selfAuthored)
        {
            return [_currentUser.Id];
        }

        List<Guid> merged = new();

        if (command.AthleteId is { } single)
        {
            merged.Add(single);
        }

        if (command.AthleteIds is { Count: > 0 })
        {
            merged.AddRange(command.AthleteIds);
        }

        // First occurrence wins and an empty id counts as none: the same shorthand the single id
        // carried before the plural existed.
        List<UserId> destinations = new();
        HashSet<Guid> seen = new();

        foreach (var athleteId in merged)
        {
            if (athleteId != Guid.Empty && seen.Add(athleteId))
            {
                destinations.Add(new UserId(athleteId));
            }
        }

        return destinations;
    }
}

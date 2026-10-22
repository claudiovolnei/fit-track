using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Points one or more athletes at a series that already exists: the plan the trainer authored stays
/// as it is, and each athlete of their roster keeps their own assignment with its own 60-day term —
/// a cycle the athlete already follows is reassigned onto it, a fresh one is opened.
/// <para>
/// Authorship rules the series and the roster rules the destinations, for every caller: only the
/// author may assign their series, and only to their own athletes. An athlete the administrator
/// released assigns their own series to themselves — the athletes of the request are ignored, the
/// same way creation ignores them.
/// </para>
/// </summary>
public sealed class AssignWorkoutSeries : IAssignWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IQueryAthletes _athletes;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly IAccountPermissions _permissions;

    public AssignWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryAthletes athletes,
        ITrainerAthleteAssignmentRepository links,
        IWorkoutSeriesAssignmentRepository assignments,
        IClock clock,
        IIdGenerator idGenerator,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _series = series;
        _athletes = athletes;
        _links = links;
        _assignments = assignments;
        _clock = clock;
        _idGenerator = idGenerator;
        _permissions = permissions;
    }

    public async Task ExecuteAsync(
        AssignWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "assign a workout series they authored",
            cancellationToken);

        var series = await WorkoutSeriesAccess.RequireOwnedAsync(
            _series,
            command.SeriesId,
            _currentUser.Id,
            cancellationToken);

        var selfAuthored = !_currentUser.IsPersonalTrainer;

        var destinations = selfAuthored
            ? WorkoutSeriesDestinations.ForCaller(_currentUser.Id)
            : WorkoutSeriesDestinations.Merge(command.AthleteId, command.AthleteIds);

        // An assignment without a destination is no prescription at all: better refused at the edge
        // than a silent no-op dressed as success.
        if (destinations.Count == 0)
        {
            throw new InvalidCommandException(
                "Assign a workout series to at least one athlete.");
        }

        if (!selfAuthored)
        {
            await WorkoutSeriesDestinations.RequirePrescribableAsync(
                _currentUser.Id,
                destinations,
                _athletes,
                _links,
                cancellationToken);
        }

        // The links the athletes follow, closed or not: each destination points at their new cycle,
        // and nothing else about the series moves.
        var existing = await _assignments.GetByUserIdsAsync(destinations, cancellationToken);
        var now = _clock.UtcNow;

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
                    now);
            }
            else
            {
                link.Reassign(series.Id, _currentUser.Id, now);
                assignment = link;
            }

            await _assignments.SaveAsync(assignment, cancellationToken);
        }
    }
}

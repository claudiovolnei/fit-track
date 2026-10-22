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
/// Creates a reusable workout series. The name is unique per trainer, checked here so the caller gets
/// a stated rule instead of the raw unique-index error. Restricted to personal trainers, who can only
/// assign the series to athletes on their own roster: the roster of the athlete side is the check.
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

    public CreateWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryWorkoutSeries query,
        IQueryAthletes athletes,
        ITrainerAthleteAssignmentRepository links,
        IWorkoutSeriesAssignmentRepository assignments,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _series = series;
        _query = query;
        _athletes = athletes;
        _links = links;
        _assignments = assignments;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(
        CreateWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("create a workout series");

        var name = new WorkoutSeriesName(command.Name);

        if (await _series.NameTakenAsync(name, _currentUser.Id, excluding: null, cancellationToken))
        {
            throw new DomainException(
                "workout_series.name.duplicate",
                $"A workout series named '{name.Value}' already exists.");
        }

        var athleteId = command.AthleteId is { } athlete && athlete != Guid.Empty
            ? new UserId(athlete)
            : (UserId?)null;

        if (athleteId is not null && !await _athletes.ExistsAsync(athleteId.Value, cancellationToken))
        {
            throw new NotFoundException($"No athlete with id '{athleteId.Value.Value}'.");
        }

        // The roster rules what a trainer can prescribe: an unlinked athlete or one under another
        // trainer is refused the same way, without revealing whose roster the athlete is on.
        if (athleteId is not null)
        {
            var roster = await _links.GetByAthleteIdAsync(athleteId.Value, cancellationToken);

            if (roster is null || roster.TrainerId != _currentUser.Id)
            {
                throw new ForbiddenException(
                    "A personal trainer only prescribes series for their own athletes.");
            }
        }

        // The link the athlete follows, closed or not: it is where the sheet of the last cycle lives.
        var existing = athleteId is null
            ? null
            : await _assignments.GetByUserIdAsync(athleteId.Value, cancellationToken);

        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_idGenerator.NewId()),
            name,
            _currentUser.Id,
            athleteId);

        await _series.SaveAsync(series, cancellationToken);

        if (command.CopyPreviousWorkouts && existing is not null)
        {
            // The prescription travels with the cycle: the trainer moves the loads, not the sheet.
            var source = await _series.GetByIdAsync(existing.SeriesId, cancellationToken);

            if (source is not null)
            {
                foreach (var workout in source.Workouts)
                {
                    series.AddWorkout(
                        new WorkoutSeriesWorkoutId(_idGenerator.NewId()),
                        workout.Name,
                        [.. workout.Exercises]);
                }

                // The workouts are rows of their own: they are inserted by the same path the
                // trainer uses when composing a series by hand.
                await _series.SaveAsync(series, cancellationToken);
            }
        }

        // When the series is created for a specific athlete, the link and the 60-day term start
        // right away: there is no separate "assign" step anymore. If the athlete already had a
        // series, the existing link is reused (reassign) instead of inserting a second one.
        if (athleteId is not null)
        {
            WorkoutSeriesAssignment assignment;

            if (existing is null)
            {
                assignment = WorkoutSeriesAssignment.Assign(
                    new WorkoutSeriesAssignmentId(_idGenerator.NewId()),
                    series.Id,
                    athleteId.Value,
                    _currentUser.Id,
                    _clock.UtcNow);
            }
            else
            {
                existing.Reassign(series.Id, _currentUser.Id, _clock.UtcNow);
                assignment = existing;
            }

            await _assignments.SaveAsync(assignment, cancellationToken);
        }

        return await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);
    }
}

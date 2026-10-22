using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Starts today's workout from one workout of the series assigned to the athlete. It is idempotent: if
/// the workout was already started today, the existing one comes back instead of a second session.
/// A workout that is not part of the athlete's series answers as if it did not exist; once the
/// 60-day term is over, or the trainer closed the cycle early, starting a new workout is
/// refused while one already running still resumes.
/// </summary>
public sealed class StartWorkout : IStartWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public StartWorkout(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IWorkoutSeriesAssignmentRepository assignments,
        IWorkoutSessionRepository sessions,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _currentUser = currentUser;
        _series = series;
        _assignments = assignments;
        _sessions = sessions;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<StartWorkoutResult> ExecuteAsync(
        StartWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        var workoutId = new WorkoutSeriesWorkoutId(command.WorkoutSeriesWorkoutId);
        var now = _clock.UtcNow;

        var assignment = await _assignments.GetByUserIdAsync(_currentUser.Id, cancellationToken);
        var series = assignment is null || !assignment.IsOpen
            ? null
            : await _series.GetByIdAsync(assignment.SeriesId, cancellationToken);

        if (series is null || series.Workouts.All(workout => workout.Id != workoutId))
        {
            throw new NotFoundException($"Workout '{command.WorkoutSeriesWorkoutId}' was not found.");
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var existing = await _sessions.GetByPlannedWorkoutAsync(_currentUser.Id, today, workoutId, cancellationToken);

        if (existing is not null)
        {
            return new StartWorkoutResult(existing.ToDto(now), Started: false);
        }

        if (assignment!.IsExpiredAt(now))
        {
            throw new DomainException(
                "workout_series.assignment.expired",
                "The assigned workout series has expired.");
        }

        var session = WorkoutSession.StartFromPlannedWorkout(
            new WorkoutSessionId(_idGenerator.NewId()),
            _currentUser.Id,
            new WorkoutDate(today),
            now,
            workoutId);

        await _sessions.SaveAsync(session, cancellationToken);

        return new StartWorkoutResult(session.ToDto(now), Started: true);
    }
}

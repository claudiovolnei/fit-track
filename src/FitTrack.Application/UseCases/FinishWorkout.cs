using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Stops the timer and closes the workout. The instant comes from the clock port, and finishing twice
/// is refused with the aggregate's own code. The workout only closes with every exercise of the
/// prescription ticked: the rule is checked here because the prescription lives in the series context
/// while the session only keeps the chosen workout by id, with no foreign key.
/// </summary>
public sealed class FinishWorkout : IFinishWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public FinishWorkout(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IWorkoutSessionRepository sessions,
        IClock clock)
    {
        _currentUser = currentUser;
        _series = series;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<WorkoutSessionDto> ExecuteAsync(
        FinishWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        var session = await WorkoutSessionAccess.RequireOwnedAsync(
            _sessions,
            command.WorkoutSessionId,
            _currentUser.Id,
            cancellationToken);

        await RequireEveryExerciseTickedAsync(session, cancellationToken);

        var now = _clock.UtcNow;

        session.Finish(now);

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(now);
    }

    /// <summary>
    /// Every exercise the workout prescribes has to be ticked before the workout closes. A workout
    /// whose prescription is gone — the trainer removed it while the athlete was training — closes
    /// without the rule: there is nothing left to tick.
    /// </summary>
    private async Task RequireEveryExerciseTickedAsync(
        WorkoutSession session,
        CancellationToken cancellationToken)
    {
        if (session.PlannedWorkoutId is not { } workoutId)
        {
            return;
        }

        var workout = await _series.GetWorkoutAsync(workoutId, cancellationToken);

        if (workout is null)
        {
            return;
        }

        var pending = workout.Exercises
            .Where(prescribed => !session.IsExerciseCompleted(prescribed.ExerciseId))
            .Select(prescribed => prescribed.ExerciseId)
            .ToList();

        if (pending.Count > 0)
        {
            throw new ConflictException(
                "session.finish.exercises.pending",
                "Every exercise of the workout has to be ticked before it closes.");
        }
    }
}

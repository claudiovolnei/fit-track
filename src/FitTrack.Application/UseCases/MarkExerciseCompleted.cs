using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Ticks one exercise of the workout in progress as done, or unticks it. The exercise has to be one
/// the trainer prescribed — the same rule a set obeys — and the tick is what lets the athlete close
/// the workout with every exercise done.
/// </summary>
public sealed class MarkExerciseCompleted : IMarkExerciseCompleted
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public MarkExerciseCompleted(
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
        MarkExerciseCompletedCommand command,
        CancellationToken cancellationToken)
    {
        var session = await WorkoutSessionAccess.RequireOwnedAsync(
            _sessions,
            command.WorkoutSessionId,
            _currentUser.Id,
            cancellationToken);

        var exerciseId = new ExerciseId(command.ExerciseId);

        await RequirePrescribedAsync(session, exerciseId, cancellationToken);

        if (command.Completed)
        {
            session.MarkExerciseCompleted(exerciseId);
        }
        else
        {
            session.UnmarkExerciseCompleted(exerciseId);
        }

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(_clock.UtcNow);
    }

    /// <summary>
    /// The prescribed exercise the tick belongs to. The rule is checked here because the prescription
    /// lives in the series context while the session only keeps the chosen workout by id, with no
    /// foreign key: a tick outside the prescription is refused before the aggregate is touched.
    /// </summary>
    private async Task RequirePrescribedAsync(
        WorkoutSession session,
        ExerciseId exerciseId,
        CancellationToken cancellationToken)
    {
        if (session.PlannedWorkoutId is not { } workoutId)
        {
            throw new DomainException(
                "session.set.workout.required",
                "A workout session must come from a workout of a series.");
        }

        var workout = await _series.GetWorkoutAsync(workoutId, cancellationToken);

        if (workout is null)
        {
            throw new DomainException(
                "session.set.workout.not_found",
                "The workout this session came from is no longer available.");
        }

        foreach (var candidate in workout.Exercises)
        {
            if (candidate.ExerciseId == exerciseId)
            {
                return;
            }
        }

        throw new DomainException(
            "session.set.exercise.not_prescribed",
            "The exercise is not part of the prescribed workout.");
    }
}

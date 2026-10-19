using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Registers a set the athlete performed in the workout in progress. The aggregate refuses the set
/// once the workout is finished, so a stale screen cannot add to a closed workout. The exercise has to
/// be one the trainer prescribed and the repetitions come from that prescription: the athlete only
/// fills in the load.
/// </summary>
public sealed class LogPerformedSet : ILogPerformedSet
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public LogPerformedSet(
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
        LogPerformedSetCommand command,
        CancellationToken cancellationToken)
    {
        var session = await WorkoutSessionAccess.RequireOwnedAsync(
            _sessions,
            command.WorkoutSessionId,
            _currentUser.Id,
            cancellationToken);

        var prescribed = await RequirePrescribedAsync(
            session,
            new ExerciseId(command.ExerciseId),
            cancellationToken);

        session.RegisterSet(
            prescribed.ExerciseId,
            new Load(command.LoadKilograms),
            new Repetitions(prescribed.Repetitions));

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(_clock.UtcNow);
    }

    /// <summary>
    /// The prescribed exercise the set belongs to. The rule is checked here because the prescription
    /// lives in the series context while the session only keeps the chosen workout by id, with no foreign
    /// key: a set outside the prescription is refused before the aggregate is touched.
    /// </summary>
    private async Task<PrescribedExercise> RequirePrescribedAsync(
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
                return candidate;
            }
        }

        throw new DomainException(
            "session.set.exercise.not_prescribed",
            "The exercise is not part of the prescribed workout.");
    }
}

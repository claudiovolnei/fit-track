using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Registers a set the athlete performed in the workout in progress. The aggregate refuses the set
/// once the workout is finished, so a stale screen cannot add to a closed workout.
/// </summary>
public sealed class LogPerformedSet : ILogPerformedSet
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public LogPerformedSet(
        ICurrentUser currentUser,
        IWorkoutSessionRepository sessions,
        IClock clock)
    {
        _currentUser = currentUser;
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

        session.RegisterSet(
            new ExerciseId(command.ExerciseId),
            new Load(command.LoadKilograms),
            new Repetitions(command.Repetitions));

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(_clock.UtcNow);
    }
}

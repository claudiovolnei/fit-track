using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Discards a workout the athlete started by mistake. Only while it is running and has no sets — once
/// something was performed, the record stays and the wrong workout is not silently erased.
/// </summary>
public sealed class CancelWorkout : ICancelWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSessionRepository _sessions;

    public CancelWorkout(ICurrentUser currentUser, IWorkoutSessionRepository sessions)
    {
        _currentUser = currentUser;
        _sessions = sessions;
    }

    public async Task ExecuteAsync(CancelWorkoutCommand command, CancellationToken cancellationToken)
    {
        var session = await WorkoutSessionAccess.RequireOwnedAsync(
            _sessions,
            command.WorkoutSessionId,
            _currentUser.Id,
            cancellationToken);

        if (!session.IsRunning || session.HasSets)
        {
            throw new InvalidCommandException("Only a running workout without sets can be cancelled.");
        }

        await _sessions.RemoveAsync(session, cancellationToken);
    }
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Stops the timer and closes the workout. The instant comes from the clock port, and finishing twice
/// is refused with the aggregate's own code.
/// </summary>
public sealed class FinishWorkout : IFinishWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public FinishWorkout(
        ICurrentUser currentUser,
        IWorkoutSessionRepository sessions,
        IClock clock)
    {
        _currentUser = currentUser;
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

        var now = _clock.UtcNow;

        session.Finish(now);

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(now);
    }
}

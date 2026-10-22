using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Closes sessions that have been running for at least 2.5 hours, so a forgotten workout stops
/// blocking the athlete's day. The session becomes history with its duration capped at 2.5 hours
/// and no intensity, since the system cannot guess how the athlete felt.
/// </summary>
public sealed class AutoFinishExpiredWorkouts : IAutoFinishExpiredWorkouts
{
    private static readonly TimeSpan Expiry = TimeSpan.FromHours(2.5);

    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public AutoFinishExpiredWorkouts(IWorkoutSessionRepository sessions, IClock clock)
    {
        _sessions = sessions;
        _clock = clock;
    }

    public AutoFinishExpiredWorkouts(IClock clock, IWorkoutSessionRepository sessions)
        : this(sessions, clock)
    {
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var cutoff = now - Expiry;

        var expired = await _sessions.ListRunningStartedBeforeAsync(cutoff, cancellationToken);

        var finished = 0;

        foreach (var session in expired)
        {
            try
            {
                session.Finish(session.StartedAt + Expiry, null, null);
                await _sessions.SaveAsync(session, cancellationToken);
                finished++;
            }
            catch (Domain.Common.DomainException)
            {
                // Already finished (race with the athlete or another worker): nothing to do.
            }
        }

        return finished;
    }
}
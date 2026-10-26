using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Closes sessions that have been running for at least 2.5 hours, so a forgotten workout stops
/// blocking the athlete's day. The session becomes history with its duration capped at 2.5 hours
/// and no intensity, since the system cannot guess how the athlete felt. When the session came
/// from a prescribed workout, it records the prescribed sets so the history reflects the plan.
/// </summary>
public sealed class AutoFinishExpiredWorkouts : IAutoFinishExpiredWorkouts
{
    private static readonly TimeSpan Expiry = TimeSpan.FromHours(2.5);

    private readonly IWorkoutSessionRepository _sessions;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IClock _clock;

    public AutoFinishExpiredWorkouts(
        IWorkoutSessionRepository sessions,
        IWorkoutSeriesRepository series,
        IClock clock)
    {
        _sessions = sessions;
        _series = series;
        _clock = clock;
    }

    public AutoFinishExpiredWorkouts(
        IClock clock,
        IWorkoutSessionRepository sessions,
        IWorkoutSeriesRepository series)
        : this(sessions, series, clock)
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
                if (session.PlannedWorkoutId is { } workoutId)
                {
                    var workout = await _series.GetWorkoutAsync(workoutId, cancellationToken);
                    if (workout is not null)
                    {
                        foreach (var prescribed in workout.Exercises)
                        {
                            for (var i = 0; i < prescribed.Sets; i++)
                            {
                                var load = prescribed.LoadKilograms is { } kg ? new Load(kg) : new Load(0);
                                session.RegisterSet(
                                    prescribed.ExerciseId,
                                    load,
                                    new Repetitions(prescribed.Repetitions));
                            }
                        }
                    }
                }

                session.Finish(session.StartedAt + Expiry, null, null);
                await _sessions.SaveAsync(session, cancellationToken);
                finished++;
            }
            catch (DomainException)
            {
                // Already finished (race with the athlete or another worker): nothing to do.
                // If the prescription is missing, the job continues (log warning if needed).
            }
        }

        return finished;
    }
}

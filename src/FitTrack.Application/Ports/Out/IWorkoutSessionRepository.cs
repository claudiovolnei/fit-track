using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>Write-side access to the <see cref="WorkoutSession"/> aggregate.</summary>
public interface IWorkoutSessionRepository
{
    Task<WorkoutSession?> GetByIdAsync(WorkoutSessionId id, CancellationToken cancellationToken);

    /// <summary>
    /// Today's workout already started from that series workout, if any. It is what makes starting
    /// idempotent; under a race the partial unique index in the database is the backstop.
    /// </summary>
    Task<WorkoutSession?> GetByPlannedWorkoutAsync(
        UserId userId,
        DateOnly workoutDate,
        WorkoutSeriesWorkoutId workoutId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every session the athlete has on that date, running or finished. It is what the rule of one
    /// workout per day is checked against: the rule belongs to the use case, because the database may
    /// still hold more than one workout on a day from before the rule existed.
    /// </summary>
    Task<IReadOnlyList<WorkoutSession>> ListByDateAsync(
        UserId userId,
        DateOnly workoutDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every session still running whose start is at or before <paramref name="cutoff"/>. Used by
    /// the auto-finish job and by the lazy guard in <c>StartWorkout</c>.
    /// </summary>
    Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        UserId userId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);

    Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken);

    Task RemoveAsync(WorkoutSession session, CancellationToken cancellationToken);
}

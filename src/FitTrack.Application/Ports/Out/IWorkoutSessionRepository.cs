using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>Write-side access to the <see cref="WorkoutSession"/> aggregate.</summary>
public interface IWorkoutSessionRepository
{
    Task<WorkoutSession?> GetByIdAsync(WorkoutSessionId id, CancellationToken cancellationToken);

    /// <summary>
    /// Today's workout already started from that option, if any. It is what makes starting
    /// idempotent; under a race the partial unique index in the database is the backstop.
    /// </summary>
    Task<WorkoutSession?> GetByPlannedWorkoutAsync(
        UserId userId,
        DateOnly workoutDate,
        WorkoutPlanOptionId optionId,
        CancellationToken cancellationToken);

    Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken);

    Task RemoveAsync(WorkoutSession session, CancellationToken cancellationToken);
}

using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Read-side port for the plan screens. Every projection orders the options by position — the cut-1
/// lesson was ordering by a random key and returning a scrambled workout.
/// </summary>
public interface IQueryWorkoutPlans
{
    Task<IReadOnlyList<WorkoutPlanDto>> ListByTrainerAsync(UserId trainerId, CancellationToken cancellationToken);

    Task<WorkoutPlanDto?> GetByIdAsync(WorkoutPlanId planId, CancellationToken cancellationToken);

    /// <summary>
    /// The athlete's plan with the state of <paramref name="today"/> per option. <paramref name="now"/>
    /// comes from the clock port so the elapsed seconds are computed at a known instant.
    /// </summary>
    Task<WorkoutPlanDto?> GetByAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

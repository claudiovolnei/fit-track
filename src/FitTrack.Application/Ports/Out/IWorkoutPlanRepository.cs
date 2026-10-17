using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="WorkoutPlan"/> aggregate. <see cref="GetByOptionIdAsync"/>
/// exists because both actors arrive through an option: the trainer edits an option, the athlete
/// starts a workout from one, and each needs the plan that owns it.
/// </summary>
public interface IWorkoutPlanRepository
{
    Task<WorkoutPlan?> GetByIdAsync(WorkoutPlanId id, CancellationToken cancellationToken);

    Task<WorkoutPlan?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    Task<WorkoutPlan?> GetByOptionIdAsync(WorkoutPlanOptionId optionId, CancellationToken cancellationToken);

    Task SaveAsync(WorkoutPlan plan, CancellationToken cancellationToken);
}

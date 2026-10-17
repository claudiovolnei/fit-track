namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// Identifies one option inside a plan. The athlete's workout session references this id,
/// so the option survives even if the plan around it changes.
/// </summary>
public readonly record struct WorkoutPlanOptionId(Guid Value)
{
    public static WorkoutPlanOptionId New() => new(Guid.NewGuid());
}

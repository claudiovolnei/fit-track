namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>Identifies the training plan a personal trainer assigned to one athlete.</summary>
public readonly record struct WorkoutPlanId(Guid Value)
{
    public static WorkoutPlanId New() => new(Guid.NewGuid());
}

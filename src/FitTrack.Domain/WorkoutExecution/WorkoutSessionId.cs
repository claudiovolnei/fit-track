namespace FitTrack.Domain.WorkoutExecution;

/// <summary>Identifies a workout session aggregate.</summary>
public readonly record struct WorkoutSessionId(Guid Value)
{
    public static WorkoutSessionId New() => new(Guid.NewGuid());
}

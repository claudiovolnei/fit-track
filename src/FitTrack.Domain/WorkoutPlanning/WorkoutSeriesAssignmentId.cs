namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>Identifies the link that puts one workout series in the hands of one athlete.</summary>
public readonly record struct WorkoutSeriesAssignmentId(Guid Value)
{
    public static WorkoutSeriesAssignmentId New() => new(Guid.NewGuid());
}

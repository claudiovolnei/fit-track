namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// Identifies one specific workout inside a series. The execution context references it instead of
/// the whole workout, so editing the series never rewrites what someone already trained.
/// </summary>
public readonly record struct WorkoutSeriesWorkoutId(Guid Value)
{
    public static WorkoutSeriesWorkoutId New() => new(Guid.NewGuid());
}

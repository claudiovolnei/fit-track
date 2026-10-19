namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>Identifies a workout series authored by one personal trainer.</summary>
public readonly record struct WorkoutSeriesId(Guid Value)
{
    public static WorkoutSeriesId New() => new(Guid.NewGuid());
}

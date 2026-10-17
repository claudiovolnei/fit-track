namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>
/// Identifies a catalog exercise. Shared with the WorkoutExecution context,
/// which references it instead of the <see cref="Exercise"/> aggregate.
/// </summary>
public readonly record struct ExerciseId(Guid Value)
{
    public static ExerciseId New() => new(Guid.NewGuid());
}

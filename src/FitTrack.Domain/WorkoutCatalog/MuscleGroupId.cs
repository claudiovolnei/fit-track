namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>
/// Identifies a muscle group of the catalog. Exercises point at it, and the training plan
/// refers to it by id instead of a free-form label.
/// </summary>
public readonly record struct MuscleGroupId(Guid Value)
{
    public static MuscleGroupId New() => new(Guid.NewGuid());
}

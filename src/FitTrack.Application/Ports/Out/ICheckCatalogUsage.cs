using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Tells whether catalog members are still referenced by other bounded contexts. Deletion is
/// refused while any prescription (workout planning) or performed set (workout execution)
/// carries the exercise, or while the muscle group still owns exercises.
/// </summary>
public interface ICheckCatalogUsage
{
    /// <summary>Whether the exercise appears in a prescribed workout or in a performed set.</summary>
    Task<bool> ExerciseReferencedAsync(ExerciseId exerciseId, CancellationToken cancellationToken);

    /// <summary>Whether any exercise of the catalog belongs to the muscle group.</summary>
    Task<bool> MuscleGroupHasExercisesAsync(MuscleGroupId muscleGroupId, CancellationToken cancellationToken);
}

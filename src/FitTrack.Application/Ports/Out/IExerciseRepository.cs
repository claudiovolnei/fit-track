using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Ports.Out;

/// <summary>Write-side access to the <see cref="Exercise"/> aggregate. One repository per aggregate root.</summary>
public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(ExerciseId id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the exercises a caller referenced by id. Used where a whole prescription arrives at once,
    /// so an unknown reference is refused before anything is written.
    /// </summary>
    Task<IReadOnlyList<Exercise>> GetByIdsAsync(
        IReadOnlyList<ExerciseId> ids,
        CancellationToken cancellationToken);

    Task SaveAsync(Exercise exercise, CancellationToken cancellationToken);
}

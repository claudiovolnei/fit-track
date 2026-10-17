using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Ports.Out;

/// <summary>Write-side access to the <see cref="Exercise"/> aggregate. One repository per aggregate root.</summary>
public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(ExerciseId id, CancellationToken cancellationToken);

    Task SaveAsync(Exercise exercise, CancellationToken cancellationToken);
}

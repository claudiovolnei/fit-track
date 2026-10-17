using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Read-side port for catalog screens. Kept apart from the write repository so the
/// repository does not grow endless query methods.
/// </summary>
public interface IQueryExercises
{
    Task<IReadOnlyList<ExerciseDto>> ListAsync(CancellationToken cancellationToken);
}

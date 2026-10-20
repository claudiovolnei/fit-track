using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Read-side port for catalog screens. Kept apart from the write repository so the
/// repository does not grow endless query methods.
/// </summary>
public interface IQueryExercises
{
    Task<IReadOnlyList<ExerciseDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One page of the catalog, filtered by name or muscle group name and, when given, by the
    /// <paramref name="muscleGroupId"/> chip. The whole filtered collection comes back when the query
    /// is not paginated.
    /// </summary>
    Task<PagedResult<ExerciseDto>> ListAsync(
        ListQuery query,
        Guid? muscleGroupId,
        CancellationToken cancellationToken);
}

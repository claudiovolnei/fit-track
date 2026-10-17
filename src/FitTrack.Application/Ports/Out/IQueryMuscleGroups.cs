using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Read-side port for the catalog screens that offer a muscle group to pick.
/// Kept apart from the write repository, as with the exercises.
/// </summary>
public interface IQueryMuscleGroups
{
    Task<IReadOnlyList<MuscleGroupDto>> ListAsync(CancellationToken cancellationToken);
}

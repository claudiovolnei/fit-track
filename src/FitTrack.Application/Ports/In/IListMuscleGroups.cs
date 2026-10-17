using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the muscle groups of the catalog.</summary>
public interface IListMuscleGroups
{
    Task<IReadOnlyList<MuscleGroupDto>> ExecuteAsync(CancellationToken cancellationToken);
}

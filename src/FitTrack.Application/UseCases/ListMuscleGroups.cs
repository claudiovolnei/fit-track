using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the muscle groups of the catalog for any authenticated caller.</summary>
public sealed class ListMuscleGroups : IListMuscleGroups
{
    private readonly IQueryMuscleGroups _query;

    public ListMuscleGroups(IQueryMuscleGroups query) => _query = query;

    public Task<IReadOnlyList<MuscleGroupDto>> ExecuteAsync(CancellationToken cancellationToken) =>
        _query.ListAsync(cancellationToken);
}

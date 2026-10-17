using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for the muscle group catalog.</summary>
internal sealed class InMemoryMuscleGroupQueries : IQueryMuscleGroups
{
    private readonly List<MuscleGroupDto> _muscleGroups = [];

    public void Seed(params MuscleGroupDto[] muscleGroups) => _muscleGroups.AddRange(muscleGroups);

    public Task<IReadOnlyList<MuscleGroupDto>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MuscleGroupDto>>(_muscleGroups);
}

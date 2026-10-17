using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Unit.Fakes;

/// <summary>In-memory write repository honouring the <see cref="IMuscleGroupRepository"/> contract.</summary>
internal sealed class InMemoryMuscleGroupRepository : IMuscleGroupRepository
{
    private readonly Dictionary<MuscleGroupId, MuscleGroup> _byId = [];

    public IReadOnlyCollection<MuscleGroup> All => _byId.Values;

    public Task<MuscleGroup?> GetByIdAsync(MuscleGroupId id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var muscleGroup) ? muscleGroup : null);

    public Task<IReadOnlyList<MuscleGroup>> GetByIdsAsync(
        IReadOnlyList<MuscleGroupId> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MuscleGroup>>(
            [.. ids.Where(_byId.ContainsKey).Select(id => _byId[id])]);

    public Task<bool> NameTakenAsync(
        MuscleGroupName name,
        MuscleGroupId? excluding,
        CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.Any(muscleGroup =>
            muscleGroup.Name.Value == name.Value && (excluding is null || muscleGroup.Id != excluding.Value)));

    public Task SaveAsync(MuscleGroup muscleGroup, CancellationToken cancellationToken)
    {
        _byId[muscleGroup.Id] = muscleGroup;

        return Task.CompletedTask;
    }
}

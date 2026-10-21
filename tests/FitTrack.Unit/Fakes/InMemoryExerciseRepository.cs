using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Unit.Fakes;

/// <summary>In-memory write repository honouring the <see cref="IExerciseRepository"/> contract.</summary>
internal sealed class InMemoryExerciseRepository : IExerciseRepository
{
    private readonly Dictionary<ExerciseId, Exercise> _byId = [];

    public IReadOnlyCollection<Exercise> All => _byId.Values;

    public Task<Exercise?> GetByIdAsync(ExerciseId id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var exercise) ? exercise : null);

    public Task<IReadOnlyList<Exercise>> GetByIdsAsync(
        IReadOnlyList<ExerciseId> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Exercise>>(
            [.. ids.Where(_byId.ContainsKey).Select(id => _byId[id])]);

    public Task SaveAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        _byId[exercise.Id] = exercise;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        _byId.Remove(exercise.Id);

        return Task.CompletedTask;
    }
}

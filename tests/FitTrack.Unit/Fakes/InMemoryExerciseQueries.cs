using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for the exercise catalog.</summary>
internal sealed class InMemoryExerciseQueries : IQueryExercises
{
    private readonly List<ExerciseDto> _exercises = [];

    public void Seed(params ExerciseDto[] exercises) => _exercises.AddRange(exercises);

    public Task<IReadOnlyList<ExerciseDto>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ExerciseDto>>(_exercises);

    public Task<PagedResult<ExerciseDto>> ListAsync(
        ListQuery query,
        Guid? muscleGroupId,
        CancellationToken cancellationToken)
    {
        var source = muscleGroupId is { } groupId && groupId != Guid.Empty
            ? _exercises.Where(exercise => exercise.MuscleGroupId == groupId)
            : _exercises;

        return Task.FromResult(FakePaging.Apply(
            source.OrderBy(exercise => exercise.Name),
            query,
            exercise => $"{exercise.Name} {exercise.MuscleGroupName}"));
    }
}

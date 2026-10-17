using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the exercise catalog for any authenticated caller.</summary>
public sealed class ListExercises : IListExercises
{
    private readonly IQueryExercises _query;

    public ListExercises(IQueryExercises query) => _query = query;

    public Task<IReadOnlyList<ExerciseDto>> ExecuteAsync(CancellationToken cancellationToken) =>
        _query.ListAsync(cancellationToken);
}

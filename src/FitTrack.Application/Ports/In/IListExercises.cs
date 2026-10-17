using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the exercise catalog.</summary>
public interface IListExercises
{
    Task<IReadOnlyList<ExerciseDto>> ExecuteAsync(CancellationToken cancellationToken);
}

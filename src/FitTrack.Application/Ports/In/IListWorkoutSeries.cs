using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the series the personal trainer authored.</summary>
public interface IListWorkoutSeries
{
    Task<IReadOnlyList<WorkoutSeriesDto>> ExecuteAsync(CancellationToken cancellationToken);
}

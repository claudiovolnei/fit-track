using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for renaming a series.</summary>
public interface IRenameWorkoutSeries
{
    Task<WorkoutSeriesDto> ExecuteAsync(RenameWorkoutSeriesCommand command, CancellationToken cancellationToken);
}

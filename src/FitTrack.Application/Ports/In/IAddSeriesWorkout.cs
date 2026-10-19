using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for adding a specific workout to a series.</summary>
public interface IAddSeriesWorkout
{
    Task<WorkoutSeriesWorkoutDto> ExecuteAsync(AddSeriesWorkoutCommand command, CancellationToken cancellationToken);
}

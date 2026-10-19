using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for replacing the name and prescription of one workout of a series.</summary>
public interface IChangeSeriesWorkout
{
    Task<WorkoutSeriesWorkoutDto> ExecuteAsync(ChangeSeriesWorkoutCommand command, CancellationToken cancellationToken);
}

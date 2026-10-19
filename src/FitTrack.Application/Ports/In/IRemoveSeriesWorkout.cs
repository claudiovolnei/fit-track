using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for dropping a workout from a series.</summary>
public interface IRemoveSeriesWorkout
{
    Task ExecuteAsync(RemoveSeriesWorkoutCommand command, CancellationToken cancellationToken);
}

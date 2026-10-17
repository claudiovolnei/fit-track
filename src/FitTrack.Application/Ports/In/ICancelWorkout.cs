using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for discarding a workout the athlete started by mistake.</summary>
public interface ICancelWorkout
{
    Task ExecuteAsync(CancelWorkoutCommand command, CancellationToken cancellationToken);
}

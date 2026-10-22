using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for a personal trainer (or admin) cancelling an athlete's running workout.</summary>
public interface ICancelAthleteWorkout
{
    Task ExecuteAsync(CancelAthleteWorkoutCommand command, CancellationToken cancellationToken);
}
using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

public interface IDeleteAthleteWorkout
{
    Task ExecuteAsync(DeleteAthleteWorkoutCommand command, CancellationToken cancellationToken);
}

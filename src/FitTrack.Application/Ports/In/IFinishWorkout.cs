using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for stopping the timer and closing the athlete's workout.</summary>
public interface IFinishWorkout
{
    Task<WorkoutSessionDto> ExecuteAsync(FinishWorkoutCommand command, CancellationToken cancellationToken);
}

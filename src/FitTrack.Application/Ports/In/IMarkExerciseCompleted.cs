using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for ticking an exercise of the workout in progress as done.</summary>
public interface IMarkExerciseCompleted
{
    Task<WorkoutSessionDto> ExecuteAsync(MarkExerciseCompletedCommand command, CancellationToken cancellationToken);
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for starting today's workout from one workout of the assigned series.</summary>
public interface IStartWorkout
{
    Task<StartWorkoutResult> ExecuteAsync(StartWorkoutCommand command, CancellationToken cancellationToken);
}

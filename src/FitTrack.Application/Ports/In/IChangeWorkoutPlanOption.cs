using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for changing the muscle groups of one plan option.</summary>
public interface IChangeWorkoutPlanOption
{
    Task<WorkoutPlanOptionDto> ExecuteAsync(
        ChangeWorkoutPlanOptionCommand command,
        CancellationToken cancellationToken);
}

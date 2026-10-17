using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for adding an option to a plan.</summary>
public interface IAddWorkoutPlanOption
{
    Task<WorkoutPlanOptionDto> ExecuteAsync(
        AddWorkoutPlanOptionCommand command,
        CancellationToken cancellationToken);
}

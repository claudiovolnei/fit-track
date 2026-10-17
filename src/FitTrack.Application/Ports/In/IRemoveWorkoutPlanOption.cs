using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for dropping an option from a plan.</summary>
public interface IRemoveWorkoutPlanOption
{
    Task ExecuteAsync(RemoveWorkoutPlanOptionCommand command, CancellationToken cancellationToken);
}

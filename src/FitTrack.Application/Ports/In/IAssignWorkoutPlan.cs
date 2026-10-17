using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for assigning a training plan to an athlete.</summary>
public interface IAssignWorkoutPlan
{
    Task<AssignWorkoutPlanResult> ExecuteAsync(
        AssignWorkoutPlanCommand command,
        CancellationToken cancellationToken);
}

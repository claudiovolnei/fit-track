using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for reading one plan the personal trainer authored.</summary>
public interface IGetWorkoutPlan
{
    Task<WorkoutPlanDto> ExecuteAsync(Guid planId, CancellationToken cancellationToken);
}

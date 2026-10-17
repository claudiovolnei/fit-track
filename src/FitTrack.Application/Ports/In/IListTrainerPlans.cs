using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the plans the personal trainer authored.</summary>
public interface IListTrainerPlans
{
    Task<IReadOnlyList<WorkoutPlanDto>> ExecuteAsync(CancellationToken cancellationToken);
}

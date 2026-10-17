using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for the athlete reading their own plan with the state of today.</summary>
public interface IGetMyWorkoutPlan
{
    /// <summary>The plan, or null when the trainer has not assigned one yet.</summary>
    Task<WorkoutPlanDto?> ExecuteAsync(CancellationToken cancellationToken);
}

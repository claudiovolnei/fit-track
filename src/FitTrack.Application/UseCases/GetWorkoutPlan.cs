using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Reads one plan. A plan authored by another trainer answers as if it did not exist, so the read
/// never reveals whose athletes the other trainer is training.
/// </summary>
public sealed class GetWorkoutPlan : IGetWorkoutPlan
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutPlans _query;

    public GetWorkoutPlan(ICurrentUser currentUser, IQueryWorkoutPlans query)
    {
        _currentUser = currentUser;
        _query = query;
    }

    public async Task<WorkoutPlanDto> ExecuteAsync(Guid planId, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("see the workout plans they authored");

        var plan = await _query.GetByIdAsync(new WorkoutPlanId(planId), cancellationToken);

        if (plan is null || plan.TrainerId != _currentUser.Id.Value)
        {
            throw new NotFoundException($"Workout plan '{planId}' was not found.");
        }

        return plan;
    }
}

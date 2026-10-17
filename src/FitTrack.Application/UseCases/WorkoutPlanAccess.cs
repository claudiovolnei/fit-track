using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// The access rules of the planning use cases. A trainer only reaches the plans they authored; a plan
/// they did not author answers as if it did not exist, so the answer never leaks someone else's plan.
/// </summary>
internal static class WorkoutPlanAccess
{
    public static async Task<WorkoutPlan> RequireOwnedAsync(
        IWorkoutPlanRepository plans,
        Guid planId,
        UserId trainerId,
        CancellationToken cancellationToken)
    {
        var plan = await plans.GetByIdAsync(new WorkoutPlanId(planId), cancellationToken)
            ?? throw new NotFoundException($"Workout plan '{planId}' was not found.");

        if (plan.TrainerId != trainerId)
        {
            throw new ForbiddenException("A personal trainer only manages the plans they authored.");
        }

        return plan;
    }
}

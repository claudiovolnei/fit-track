using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Drops an option from a plan. Workouts already started from it stay in the athlete's history:
/// the session references the option id and the removal does not cascade into the execution context.
/// Restricted to the trainer who authored the plan.
/// </summary>
public sealed class RemoveWorkoutPlanOption : IRemoveWorkoutPlanOption
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutPlanRepository _plans;

    public RemoveWorkoutPlanOption(ICurrentUser currentUser, IWorkoutPlanRepository plans)
    {
        _currentUser = currentUser;
        _plans = plans;
    }

    public async Task ExecuteAsync(RemoveWorkoutPlanOptionCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("build a workout plan");

        var plan = await WorkoutPlanAccess.RequireOwnedAsync(
            _plans,
            command.PlanId,
            _currentUser.Id,
            cancellationToken);

        plan.RemoveOption(new WorkoutPlanOptionId(command.OptionId));

        await _plans.SaveAsync(plan, cancellationToken);
    }
}

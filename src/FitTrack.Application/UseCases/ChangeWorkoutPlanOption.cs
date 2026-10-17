using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Changes which muscle groups one option covers. Editing an option never touches a workout already
/// started from it: the session keeps the option id, not a copy of these groups.
/// Restricted to the trainer who authored the plan.
/// </summary>
public sealed class ChangeWorkoutPlanOption : IChangeWorkoutPlanOption
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutPlanRepository _plans;
    private readonly IMuscleGroupRepository _muscleGroups;

    public ChangeWorkoutPlanOption(
        ICurrentUser currentUser,
        IWorkoutPlanRepository plans,
        IMuscleGroupRepository muscleGroups)
    {
        _currentUser = currentUser;
        _plans = plans;
        _muscleGroups = muscleGroups;
    }

    public async Task<WorkoutPlanOptionDto> ExecuteAsync(
        ChangeWorkoutPlanOptionCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("build a workout plan");

        var plan = await WorkoutPlanAccess.RequireOwnedAsync(
            _plans,
            command.PlanId,
            _currentUser.Id,
            cancellationToken);

        var names = await MuscleGroupLookup.NamesOfAsync(_muscleGroups, command.MuscleGroupIds, cancellationToken);
        var optionId = new WorkoutPlanOptionId(command.OptionId);

        plan.ChangeOptionMuscleGroups(
            optionId,
            [.. command.MuscleGroupIds.Select(id => new MuscleGroupId(id))]);

        await _plans.SaveAsync(plan, cancellationToken);

        return plan.FindOption(optionId).ToDto(names);
    }
}

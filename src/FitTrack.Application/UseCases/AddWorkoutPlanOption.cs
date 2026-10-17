using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>Adds an option to a plan. Restricted to the trainer who authored it.</summary>
public sealed class AddWorkoutPlanOption : IAddWorkoutPlanOption
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutPlanRepository _plans;
    private readonly IMuscleGroupRepository _muscleGroups;
    private readonly IIdGenerator _idGenerator;

    public AddWorkoutPlanOption(
        ICurrentUser currentUser,
        IWorkoutPlanRepository plans,
        IMuscleGroupRepository muscleGroups,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _plans = plans;
        _muscleGroups = muscleGroups;
        _idGenerator = idGenerator;
    }

    public async Task<WorkoutPlanOptionDto> ExecuteAsync(
        AddWorkoutPlanOptionCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("build a workout plan");

        var plan = await WorkoutPlanAccess.RequireOwnedAsync(
            _plans,
            command.PlanId,
            _currentUser.Id,
            cancellationToken);

        var names = await MuscleGroupLookup.NamesOfAsync(_muscleGroups, command.MuscleGroupIds, cancellationToken);

        var option = plan.AddOption(
            new WorkoutPlanOptionId(_idGenerator.NewId()),
            [.. command.MuscleGroupIds.Select(id => new MuscleGroupId(id))]);

        await _plans.SaveAsync(plan, cancellationToken);

        return option.ToDto(names);
    }
}

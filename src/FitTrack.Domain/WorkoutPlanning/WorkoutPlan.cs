using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// Planning aggregate root: the plan one personal trainer assigned to one athlete. The plan holds
/// up to <see cref="MaxOptions"/> options, each one a set of muscle groups; the athlete decides
/// which option to train on a given day.
/// </summary>
public sealed class WorkoutPlan
{
    public const int MaxOptions = 7;

    private readonly List<WorkoutPlanOption> _options = [];

    private WorkoutPlan(WorkoutPlanId id, UserId userId, UserId trainerId)
    {
        Id = id;
        UserId = userId;
        TrainerId = trainerId;
    }

    public WorkoutPlanId Id { get; }

    /// <summary>The athlete the plan belongs to. One plan per athlete.</summary>
    public UserId UserId { get; }

    /// <summary>Author of the plan. Only this trainer may edit it.</summary>
    public UserId TrainerId { get; }

    /// <summary>Options in <see cref="WorkoutPlanOption.Position"/> order, from 1 to n.</summary>
    public IReadOnlyList<WorkoutPlanOption> Options => _options;

    public static WorkoutPlan Start(WorkoutPlanId id, UserId userId, UserId trainerId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new DomainException("workout_plan.athlete.required", "A workout plan must belong to an athlete.");
        }

        if (trainerId.Value == Guid.Empty)
        {
            throw new DomainException("workout_plan.trainer.required", "A workout plan must have an author.");
        }

        return new WorkoutPlan(id, userId, trainerId);
    }

    public WorkoutPlanOption AddOption(WorkoutPlanOptionId id, IReadOnlyList<MuscleGroupId> muscleGroups)
    {
        if (_options.Count >= MaxOptions)
        {
            throw new DomainException(
                "workout_plan.option.limit_reached",
                $"A workout plan holds at most {MaxOptions} options.");
        }

        var option = new WorkoutPlanOption(id, _options.Count + 1, muscleGroups);
        _options.Add(option);

        return option;
    }

    public void ChangeOptionMuscleGroups(WorkoutPlanOptionId id, IReadOnlyList<MuscleGroupId> muscleGroups) =>
        FindOption(id).ChangeMuscleGroups(muscleGroups);

    public void RemoveOption(WorkoutPlanOptionId id)
    {
        _options.Remove(FindOption(id));

        // Positions stay 1..n so the trainer's screen and the athlete's list never show a hole.
        for (var index = 0; index < _options.Count; index++)
        {
            _options[index].MoveTo(index + 1);
        }
    }

    public WorkoutPlanOption FindOption(WorkoutPlanOptionId id) =>
        _options.FirstOrDefault(option => option.Id == id)
            ?? throw new DomainException(
                "workout_plan.option.not_found",
                $"Plan option '{id.Value}' is not part of this plan.");
}

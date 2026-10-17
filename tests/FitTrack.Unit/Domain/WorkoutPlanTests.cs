using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Domain;

public sealed class WorkoutPlanTests
{
    private static readonly UserId Athlete = UserId.New();
    private static readonly UserId Trainer = UserId.New();
    private static readonly MuscleGroupId Chest = new(Guid.NewGuid());
    private static readonly MuscleGroupId Back = new(Guid.NewGuid());

    [Fact]
    public void Start_WithoutAthlete_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = WorkoutPlan.Start(WorkoutPlanId.New(), new UserId(Guid.Empty), Trainer));

        Assert.Equal("workout_plan.athlete.required", exception.Code);
    }

    [Fact]
    public void Start_WithoutTrainer_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = WorkoutPlan.Start(WorkoutPlanId.New(), Athlete, new UserId(Guid.Empty)));

        Assert.Equal("workout_plan.trainer.required", exception.Code);
    }

    [Fact]
    public void AddOption_NumbersTheOptionsFromOne()
    {
        var plan = Start();

        var first = plan.AddOption(WorkoutPlanOptionId.New(), [Chest]);
        var second = plan.AddOption(WorkoutPlanOptionId.New(), [Back]);

        Assert.Equal(1, first.Position);
        Assert.Equal(2, second.Position);
        Assert.Equal([Chest, Back], first.MuscleGroupIds.Concat(second.MuscleGroupIds));
    }

    [Fact]
    public void AddOption_WithoutMuscleGroups_Fails()
    {
        var plan = Start();

        var exception = Assert.Throws<DomainException>(() =>
            plan.AddOption(WorkoutPlanOptionId.New(), []));

        Assert.Equal("workout_plan.option.muscle_groups.empty", exception.Code);
    }

    [Fact]
    public void AddOption_BeyondSeven_Fails()
    {
        var plan = Start();

        for (var index = 1; index <= WorkoutPlan.MaxOptions; index++)
        {
            plan.AddOption(WorkoutPlanOptionId.New(), [Chest]);
        }

        var exception = Assert.Throws<DomainException>(() =>
            plan.AddOption(WorkoutPlanOptionId.New(), [Back]));

        Assert.Equal("workout_plan.option.limit_reached", exception.Code);
    }

    [Fact]
    public void ChangeOptionMuscleGroups_ReplacesTheGroups()
    {
        var plan = Start();
        var option = plan.AddOption(WorkoutPlanOptionId.New(), [Chest]);

        plan.ChangeOptionMuscleGroups(option.Id, [Back, Chest]);

        Assert.Equal([Back, Chest], option.MuscleGroupIds);
    }

    [Fact]
    public void ChangeOptionMuscleGroups_ToEmpty_Fails()
    {
        var plan = Start();
        var option = plan.AddOption(WorkoutPlanOptionId.New(), [Chest]);

        var exception = Assert.Throws<DomainException>(() =>
            plan.ChangeOptionMuscleGroups(option.Id, []));

        Assert.Equal("workout_plan.option.muscle_groups.empty", exception.Code);
        Assert.Equal([Chest], option.MuscleGroupIds);
    }

    [Fact]
    public void ChangeOptionMuscleGroups_UnknownOption_Fails()
    {
        var plan = Start();

        var exception = Assert.Throws<DomainException>(() =>
            plan.ChangeOptionMuscleGroups(WorkoutPlanOptionId.New(), [Chest]));

        Assert.Equal("workout_plan.option.not_found", exception.Code);
    }

    [Fact]
    public void RemoveOption_RenumbersTheRest()
    {
        var plan = Start();
        var first = plan.AddOption(WorkoutPlanOptionId.New(), [Chest]);
        var second = plan.AddOption(WorkoutPlanOptionId.New(), [Back]);

        plan.RemoveOption(first.Id);

        var remaining = Assert.Single(plan.Options);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(1, remaining.Position);
        Assert.Equal(2, plan.Options.Count + 1);
    }

    [Fact]
    public void RemoveOption_UnknownOption_Fails()
    {
        var plan = Start();

        var exception = Assert.Throws<DomainException>(() =>
            plan.RemoveOption(WorkoutPlanOptionId.New()));

        Assert.Equal("workout_plan.option.not_found", exception.Code);
    }

    [Fact]
    public void FindOption_UnknownOption_Fails()
    {
        var plan = Start();

        var exception = Assert.Throws<DomainException>(() =>
            plan.FindOption(WorkoutPlanOptionId.New()));

        Assert.Equal("workout_plan.option.not_found", exception.Code);
    }

    private static WorkoutPlan Start() =>
        WorkoutPlan.Start(WorkoutPlanId.New(), Athlete, Trainer);
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class WorkoutPlanningTests
{
    private readonly InMemoryWorkoutPlanRepository _plans = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly MuscleGroupId _back = MuscleGroupId.New();

    public WorkoutPlanningTests()
    {
        SaveMuscleGroup(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")));
        SaveMuscleGroup(MuscleGroup.Create(_back, new MuscleGroupName("Back")));
        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
    }

    private InMemoryWorkoutPlanQueries Queries => new(_plans, _muscleGroups, _sessions);

    [Fact]
    public async Task AssignWorkoutPlan_SavesOnePlanForTheAthlete()
    {
        var useCase = new AssignWorkoutPlan(
            StubCurrentUser.PersonalTrainer(_trainer), _athletes, _plans, Queries, _ids);

        var result = await useCase.ExecuteAsync(new AssignWorkoutPlanCommand(_athlete.Value), CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(_athlete.Value, result.Plan.UserId);
        Assert.Equal(_trainer.Value, result.Plan.TrainerId);
        Assert.Empty(result.Plan.Options);
        Assert.Single(_plans.All);
    }

    [Fact]
    public async Task AssignWorkoutPlan_Twice_KeepsTheSamePlan()
    {
        var useCase = new AssignWorkoutPlan(
            StubCurrentUser.PersonalTrainer(_trainer), _athletes, _plans, Queries, _ids);

        var first = await useCase.ExecuteAsync(new AssignWorkoutPlanCommand(_athlete.Value), CancellationToken.None);
        var second = await useCase.ExecuteAsync(new AssignWorkoutPlanCommand(_athlete.Value), CancellationToken.None);

        Assert.False(second.Created);
        Assert.Equal(first.Plan.Id, second.Plan.Id);
        Assert.Single(_plans.All);
    }

    [Fact]
    public async Task AssignWorkoutPlan_UnknownAthlete_Fails()
    {
        var useCase = new AssignWorkoutPlan(
            StubCurrentUser.PersonalTrainer(_trainer), _athletes, _plans, Queries, _ids);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new AssignWorkoutPlanCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_plans.All);
    }

    [Fact]
    public async Task AssignWorkoutPlan_AsAthlete_Fails()
    {
        var useCase = new AssignWorkoutPlan(
            StubCurrentUser.Athlete(_trainer), _athletes, _plans, Queries, _ids);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new AssignWorkoutPlanCommand(_athlete.Value), CancellationToken.None));

        Assert.Empty(_plans.All);
    }

    [Fact]
    public async Task AddWorkoutPlanOption_AddsAnOptionWithTheMuscleGroupNames()
    {
        var plan = await SeedPlanAsync();

        var option = await AddOptionAsync(plan.Id.Value, _chest.Value, _back.Value);

        Assert.Equal(1, option.Position);
        Assert.Equal([_chest.Value, _back.Value], option.MuscleGroups.Select(group => group.Id));
        Assert.Equal(["Chest", "Back"], option.MuscleGroups.Select(group => group.Name));
        Assert.Null(option.Today);
    }

    [Fact]
    public async Task AddWorkoutPlanOption_WithoutMuscleGroups_Fails()
    {
        var plan = await SeedPlanAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => AddOptionAsync(plan.Id.Value));

        Assert.Equal("workout_plan.option.muscle_groups.empty", exception.Code);
        Assert.Empty(_plans.All.Single().Options);
    }

    [Fact]
    public async Task AddWorkoutPlanOption_UnknownMuscleGroup_Fails()
    {
        var plan = await SeedPlanAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => AddOptionAsync(plan.Id.Value, Guid.NewGuid()));

        Assert.Empty(_plans.All.Single().Options);
    }

    [Fact]
    public async Task AddWorkoutPlanOption_AsAnotherTrainer_Fails()
    {
        var plan = await SeedPlanAsync();
        var useCase = new AddWorkoutPlanOption(
            StubCurrentUser.PersonalTrainer(UserId.New()), _plans, _muscleGroups, _ids);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(
                new AddWorkoutPlanOptionCommand(plan.Id.Value, [_chest.Value]),
                CancellationToken.None));

        Assert.Empty(_plans.All.Single().Options);
    }

    [Fact]
    public async Task ChangeWorkoutPlanOption_ReplacesTheGroups()
    {
        var plan = await SeedPlanAsync();
        var option = await AddOptionAsync(plan.Id.Value, _chest.Value);

        var useCase = new ChangeWorkoutPlanOption(
            StubCurrentUser.PersonalTrainer(_trainer), _plans, _muscleGroups);

        var changed = await useCase.ExecuteAsync(
            new ChangeWorkoutPlanOptionCommand(plan.Id.Value, option.Id, [_back.Value]),
            CancellationToken.None);

        Assert.Equal([_back.Value], changed.MuscleGroups.Select(group => group.Id));
        Assert.Equal("Back", changed.MuscleGroups.Single().Name);
    }

    [Fact]
    public async Task ChangeWorkoutPlanOption_UnknownOption_Fails()
    {
        var plan = await SeedPlanAsync();

        var useCase = new ChangeWorkoutPlanOption(
            StubCurrentUser.PersonalTrainer(_trainer), _plans, _muscleGroups);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(
                new ChangeWorkoutPlanOptionCommand(plan.Id.Value, Guid.NewGuid(), [_chest.Value]),
                CancellationToken.None));

        Assert.Equal("workout_plan.option.not_found", exception.Code);
    }

    [Fact]
    public async Task RemoveWorkoutPlanOption_RenumbersTheRest()
    {
        var plan = await SeedPlanAsync();
        var first = await AddOptionAsync(plan.Id.Value, _chest.Value);
        var second = await AddOptionAsync(plan.Id.Value, _back.Value);

        var useCase = new RemoveWorkoutPlanOption(StubCurrentUser.PersonalTrainer(_trainer), _plans);

        await useCase.ExecuteAsync(
            new RemoveWorkoutPlanOptionCommand(plan.Id.Value, first.Id),
            CancellationToken.None);

        var remaining = Assert.Single(_plans.All.Single().Options);
        Assert.Equal(second.Id, remaining.Id.Value);
        Assert.Equal(1, remaining.Position);
    }

    [Fact]
    public async Task ListTrainerPlans_ReturnsOnlyThePlansOfTheTrainer()
    {
        await SeedPlanAsync();
        var other = WorkoutPlan.Start(new WorkoutPlanId(_ids.NewId()), UserId.New(), UserId.New());
        await _plans.SaveAsync(other, CancellationToken.None);

        var useCase = new ListTrainerPlans(StubCurrentUser.PersonalTrainer(_trainer), Queries);

        var plans = await useCase.ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(plans);
        Assert.Equal(_athlete.Value, only.UserId);
    }

    [Fact]
    public async Task ListTrainerPlans_AsAthlete_Fails()
    {
        var useCase = new ListTrainerPlans(StubCurrentUser.Athlete(_athlete), Queries);

        await Assert.ThrowsAsync<ForbiddenException>(() => useCase.ExecuteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetWorkoutPlan_OfAnotherTrainer_Fails()
    {
        var plan = await SeedPlanAsync();
        var useCase = new GetWorkoutPlan(StubCurrentUser.PersonalTrainer(UserId.New()), Queries);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(plan.Id.Value, CancellationToken.None));
    }

    private async Task<WorkoutPlan> SeedPlanAsync()
    {
        var plan = WorkoutPlan.Start(new WorkoutPlanId(_ids.NewId()), _athlete, _trainer);
        await _plans.SaveAsync(plan, CancellationToken.None);

        return plan;
    }

    private Task<WorkoutPlanOptionDto> AddOptionAsync(Guid planId, params Guid[] muscleGroups)
    {
        var useCase = new AddWorkoutPlanOption(
            StubCurrentUser.PersonalTrainer(_trainer), _plans, _muscleGroups, _ids);

        return useCase.ExecuteAsync(
            new AddWorkoutPlanOptionCommand(planId, muscleGroups),
            CancellationToken.None);
    }

    private void SaveMuscleGroup(MuscleGroup muscleGroup) =>
        _muscleGroups.SaveAsync(muscleGroup, CancellationToken.None).GetAwaiter().GetResult();
}

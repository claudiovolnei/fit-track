using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class WorkoutExecutionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkoutPlanRepository _plans = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly InMemoryUserSessionQueries _history = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly ExerciseId _exercise = ExerciseId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private WorkoutPlan _plan = null!;
    private WorkoutPlanOption _option = null!;

    public WorkoutExecutionTests()
    {
        _muscleGroups
            .SaveAsync(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
    }

    private InMemoryWorkoutPlanQueries Queries => new(_plans, _muscleGroups, _sessions);

    [Fact]
    public async Task StartWorkout_CreatesTodaysWorkoutBoundToTheOption()
    {
        await SeedPlanAsync();

        var result = await StartAsync(_option.Id.Value);

        Assert.True(result.Started);
        Assert.Equal(_option.Id.Value, result.Session.WorkoutPlanOptionId);
        Assert.Equal(Now, result.Session.StartedAt);
        Assert.Null(result.Session.FinishedAt);
        Assert.Equal(0, result.Session.DurationSeconds);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_Twice_ReturnsTheWorkoutAlreadyRunning()
    {
        await SeedPlanAsync();

        var first = await StartAsync(_option.Id.Value);
        var second = await StartAsync(_option.Id.Value);

        Assert.False(second.Started);
        Assert.Equal(first.Session.Id, second.Session.Id);
        Assert.Equal(Now, second.Session.StartedAt);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_UnknownOption_Fails()
    {
        await SeedPlanAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => StartAsync(Guid.NewGuid()));
        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_OnAnotherAthletesPlan_Fails()
    {
        var other = WorkoutPlan.Start(new WorkoutPlanId(_ids.NewId()), UserId.New(), _trainer);
        var option = other.AddOption(new WorkoutPlanOptionId(_ids.NewId()), [_chest]);
        await _plans.SaveAsync(other, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() => StartAsync(option.Id.Value));
        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task LogPerformedSet_AddsTheSetToTheRunningWorkout()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);

        var session = await LogSetAsync(started.Session.Id, 60m, 12);

        var set = Assert.Single(session.Sets);
        Assert.Equal(_exercise.Value, set.ExerciseId);
        Assert.Equal(60m, set.LoadKilograms);
        Assert.Equal(12, set.Repetitions);
    }

    [Fact]
    public async Task LogPerformedSet_OnAnotherAthletesWorkout_Fails()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);

        var useCase = new LogPerformedSet(
            StubCurrentUser.Athlete(UserId.New()),
            _sessions,
            _clock);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(
                new LogPerformedSetCommand(started.Session.Id, _exercise.Value, 60m, 12),
                CancellationToken.None));
    }

    [Fact]
    public async Task LogPerformedSet_AfterFinish_Fails()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);
        await FinishAsync(started.Session.Id);

        var exception = await Assert.ThrowsAsync<DomainException>(() => LogSetAsync(started.Session.Id, 60m, 12));

        Assert.Equal("session.set.after_finish", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_FreezesTheElapsedTime()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);

        var session = await FinishAsync(started.Session.Id);

        Assert.Equal(Now, session.FinishedAt);
        Assert.Equal(0, session.DurationSeconds);
        Assert.False(_sessions.All.Single().IsRunning);
    }

    [Fact]
    public async Task FinishWorkout_Twice_Fails()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);
        await FinishAsync(started.Session.Id);

        var exception = await Assert.ThrowsAsync<DomainException>(() => FinishAsync(started.Session.Id));

        Assert.Equal("session.finish.already_finished", exception.Code);
    }

    [Fact]
    public async Task CancelWorkout_RemovesAWorkoutWithoutSets()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);

        var useCase = new CancelWorkout(StubCurrentUser.Athlete(_athlete), _sessions);

        await useCase.ExecuteAsync(new CancelWorkoutCommand(started.Session.Id), CancellationToken.None);

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task CancelWorkout_WithSets_Fails()
    {
        await SeedPlanAsync();
        var started = await StartAsync(_option.Id.Value);
        await LogSetAsync(started.Session.Id, 60m, 12);

        var useCase = new CancelWorkout(StubCurrentUser.Athlete(_athlete), _sessions);

        await Assert.ThrowsAsync<InvalidCommandException>(() =>
            useCase.ExecuteAsync(new CancelWorkoutCommand(started.Session.Id), CancellationToken.None));

        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task GetMyWorkoutPlan_ShowsTodaysStatePerOption()
    {
        await SeedPlanAsync();
        var idle = _plan.AddOption(new WorkoutPlanOptionId(_ids.NewId()), [_chest]);
        await _plans.SaveAsync(_plan, CancellationToken.None);
        var started = await StartAsync(_option.Id.Value);

        var useCase = new GetMyWorkoutPlan(StubCurrentUser.Athlete(_athlete), Queries, _clock);

        var plan = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.NotNull(plan);
        var today = plan.Options.Single(option => option.Id == _option.Id.Value).Today;
        Assert.NotNull(today);
        Assert.Equal(started.Session.Id, today.WorkoutSessionId);
        Assert.True(today.IsRunning);
        Assert.Null(plan.Options.Single(option => option.Id == idle.Id.Value).Today);
    }

    [Fact]
    public async Task GetMyWorkoutPlan_WithoutAPlan_ReturnsNothing()
    {
        var useCase = new GetMyWorkoutPlan(StubCurrentUser.Athlete(_athlete), Queries, _clock);

        Assert.Null(await useCase.ExecuteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ListAthletes_AsTrainer_ReturnsTheAccounts()
    {
        var useCase = new ListAthletes(StubCurrentUser.PersonalTrainer(_trainer), _athletes);

        var athletes = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal("athlete@fittrack.local", Assert.Single(athletes).Email);
    }

    [Fact]
    public async Task ListAthletes_AsAthlete_Fails()
    {
        var useCase = new ListAthletes(StubCurrentUser.Athlete(_athlete), _athletes);

        await Assert.ThrowsAsync<ForbiddenException>(() => useCase.ExecuteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ListAthleteWorkouts_AsTrainer_ReturnsTheAthletesHistory()
    {
        _history.Seed(
            _athlete,
            new FitTrack.Application.Dtos.WorkoutSessionDto(
                Guid.NewGuid(),
                new DateOnly(2026, 1, 5),
                Now,
                null,
                90,
                WorkoutPlanOptionId.New().Value,
                []));

        var useCase = new ListAthleteWorkouts(StubCurrentUser.PersonalTrainer(_trainer), _history);

        var sessions = await useCase.ExecuteAsync(
            new ListAthleteWorkoutsCommand(_athlete.Value),
            CancellationToken.None);

        Assert.Equal(90, Assert.Single(sessions).DurationSeconds);
    }

    [Fact]
    public async Task ListAthleteWorkouts_AsAthlete_Fails()
    {
        var useCase = new ListAthleteWorkouts(StubCurrentUser.Athlete(_athlete), _history);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new ListAthleteWorkoutsCommand(_athlete.Value), CancellationToken.None));
    }

    private async Task SeedPlanAsync()
    {
        _plan = WorkoutPlan.Start(new WorkoutPlanId(_ids.NewId()), _athlete, _trainer);
        _option = _plan.AddOption(new WorkoutPlanOptionId(_ids.NewId()), [_chest]);

        await _plans.SaveAsync(_plan, CancellationToken.None);
    }

    private Task<FitTrack.Application.Dtos.StartWorkoutResult> StartAsync(Guid optionId)
    {
        var useCase = new StartWorkout(
            StubCurrentUser.Athlete(_athlete),
            _plans,
            _sessions,
            _ids,
            _clock);

        return useCase.ExecuteAsync(new StartWorkoutCommand(optionId), CancellationToken.None);
    }

    private Task<FitTrack.Application.Dtos.WorkoutSessionDto> LogSetAsync(
        Guid sessionId,
        decimal load,
        int repetitions)
    {
        var useCase = new LogPerformedSet(StubCurrentUser.Athlete(_athlete), _sessions, _clock);

        return useCase.ExecuteAsync(
            new LogPerformedSetCommand(sessionId, _exercise.Value, load, repetitions),
            CancellationToken.None);
    }

    private Task<FitTrack.Application.Dtos.WorkoutSessionDto> FinishAsync(Guid sessionId)
    {
        var useCase = new FinishWorkout(StubCurrentUser.Athlete(_athlete), _sessions, _clock);

        return useCase.ExecuteAsync(new FinishWorkoutCommand(sessionId), CancellationToken.None);
    }
}

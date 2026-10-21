using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class WorkoutSeriesExecutionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 1, 5);

    private readonly InMemoryWorkoutSeriesRepository _series = new();
    private readonly InMemoryWorkoutSeriesAssignmentRepository _assignments = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryUserSessionQueries _history = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly ExerciseId _bench = ExerciseId.New();

    public WorkoutSeriesExecutionTests()
    {
        _muscleGroups
            .SaveAsync(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        _exercises
            .SaveAsync(Exercise.Create(_bench, new ExerciseName("Bench Press"), _chest), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task StartWorkout_StartsTheSessionBoundToTheWorkout()
    {
        var (_, workout) = await SeedAssignedAsync();

        var result = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        Assert.True(result.Started);
        Assert.Equal(workout.Id.Value, result.Session.WorkoutSeriesWorkoutId);
        Assert.Equal(Today, result.Session.WorkoutDate);
        Assert.Equal(Now, result.Session.StartedAt);
        Assert.Null(result.Session.FinishedAt);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_Twice_ReturnsTheRunningSession()
    {
        var (_, workout) = await SeedAssignedAsync();

        var first = await Start(Athlete).ExecuteAsync(new StartWorkoutCommand(workout.Id.Value), CancellationToken.None);
        var second = await Start(Athlete).ExecuteAsync(new StartWorkoutCommand(workout.Id.Value), CancellationToken.None);

        Assert.False(second.Started);
        Assert.Equal(first.Session.Id, second.Session.Id);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_WorkoutOfAnotherSeries_Fails()
    {
        await SeedAssignedAsync();
        var other = await SeedSeriesAsync(_trainer, "Hipertrofia");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Start(Athlete).ExecuteAsync(new StartWorkoutCommand(other.Workouts[0].Id.Value), CancellationToken.None));

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_WithoutAnAssignment_Fails()
    {
        var series = await SeedSeriesAsync(_trainer, "Hipertrofia");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Start(Athlete).ExecuteAsync(new StartWorkoutCommand(series.Workouts[0].Id.Value), CancellationToken.None));

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_ForAnotherAthlete_Fails()
    {
        var (_, workout) = await SeedAssignedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Start(StubCurrentUser.Athlete(UserId.New())).ExecuteAsync(
                new StartWorkoutCommand(workout.Id.Value),
                CancellationToken.None));

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_ExpiredSeries_Fails()
    {
        var (_, workout) = await SeedAssignedAsync(Now.AddDays(-(WorkoutSeriesAssignment.TermInDays + 1)));

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Start(Athlete).ExecuteAsync(new StartWorkoutCommand(workout.Id.Value), CancellationToken.None));

        Assert.Equal("workout_series.assignment.expired", exception.Code);
        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_ExpiredSeriesWithARunningWorkout_ResumesIt()
    {
        var (_, workout) = await SeedAssignedAsync(Now.AddDays(-(WorkoutSeriesAssignment.TermInDays + 1)));

        var running = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            _athlete,
            new WorkoutDate(Today),
            Now.AddMinutes(-5),
            workout.Id);

        await _sessions.SaveAsync(running, CancellationToken.None);

        var result = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        Assert.False(result.Started);
        Assert.Equal(running.Id.Value, result.Session.Id);
    }

    [Fact]
    public async Task LogPerformedSet_AddsTheSetWithThePrescribedRepetitions()
    {
        var started = await StartTodayAsync();

        // The athlete's intention carries the load only: the 12 repetitions come from the prescription.
        var session = await Log(Athlete).ExecuteAsync(
            new LogPerformedSetCommand(started.Session.Id, _bench.Value, 80m),
            CancellationToken.None);

        var set = Assert.Single(session.Sets);
        Assert.Equal(_bench.Value, set.ExerciseId);
        Assert.Equal(80m, set.LoadKilograms);
        Assert.Equal(12, set.Repetitions);
    }

    [Fact]
    public async Task LogPerformedSet_WithAnExerciseOutsideThePrescription_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Log(Athlete).ExecuteAsync(
                new LogPerformedSetCommand(started.Session.Id, ExerciseId.New().Value, 80m),
                CancellationToken.None));

        Assert.Equal("session.set.exercise.not_prescribed", exception.Code);
    }

    [Fact]
    public async Task LogPerformedSet_AfterFinishing_Fails()
    {
        var started = await StartTodayAsync();

        await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Log(Athlete).ExecuteAsync(
                new LogPerformedSetCommand(started.Session.Id, _bench.Value, 80m),
                CancellationToken.None));

        Assert.Equal("session.set.after_finish", exception.Code);
    }

    [Fact]
    public async Task LogPerformedSet_OnSomebodyElsesWorkout_Fails()
    {
        var started = await StartTodayAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Log(StubCurrentUser.Athlete(UserId.New())).ExecuteAsync(
                new LogPerformedSetCommand(started.Session.Id, _bench.Value, 80m),
                CancellationToken.None));
    }

    [Fact]
    public async Task FinishWorkout_ClosesTheWorkout()
    {
        var started = await StartTodayAsync();

        var finished = await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id),
            CancellationToken.None);

        Assert.NotNull(finished.FinishedAt);
        Assert.Equal(0, finished.DurationSeconds);
    }

    [Fact]
    public async Task FinishWorkout_Twice_Fails()
    {
        var started = await StartTodayAsync();

        await Finish(Athlete).ExecuteAsync(new FinishWorkoutCommand(started.Session.Id), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(new FinishWorkoutCommand(started.Session.Id), CancellationToken.None));

        Assert.Equal("session.finish.already_finished", exception.Code);
    }

    [Fact]
    public async Task CancelWorkout_RemovesAnEmptyRunningWorkout()
    {
        var started = await StartTodayAsync();

        await Cancel(Athlete).ExecuteAsync(new CancelWorkoutCommand(started.Session.Id), CancellationToken.None);

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task CancelWorkout_WithPerformedSets_Fails()
    {
        var started = await StartTodayAsync();

        await Log(Athlete).ExecuteAsync(
            new LogPerformedSetCommand(started.Session.Id, _bench.Value, 80m),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCommandException>(() =>
            Cancel(Athlete).ExecuteAsync(new CancelWorkoutCommand(started.Session.Id), CancellationToken.None));

        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task GetMyWorkoutSeries_ShowsTheWorkoutStateForToday()
    {
        var (series, workout) = await SeedAssignedAsync();

        var started = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        var mine = await MySeries(Athlete).ExecuteAsync(CancellationToken.None);

        Assert.NotNull(mine);
        Assert.Equal(series.Id.Value, mine!.Series.Id);
        Assert.Equal("Personal trainer", mine.TrainerName);
        Assert.Equal(Now.AddDays(WorkoutSeriesAssignment.TermInDays), mine.ExpiresAt);
        Assert.False(mine.IsExpired);
        Assert.Equal(WorkoutSeriesAssignment.TermInDays, mine.DaysRemaining);

        var only = Assert.Single(mine.Series.Workouts);
        Assert.Equal("A", only.Label);
        Assert.Equal("Peito e Tríceps", only.Name);
        Assert.NotNull(only.Today);
        Assert.Equal(started.Session.Id, only.Today!.WorkoutSessionId);
        Assert.True(only.Today.IsRunning);
        Assert.Equal("Bench Press", Assert.Single(only.Exercises).ExerciseName);
    }

    [Fact]
    public async Task GetMyWorkoutSeries_WithoutAnAssignment_ReturnsNull()
    {
        var mine = await MySeries(Athlete).ExecuteAsync(CancellationToken.None);

        Assert.Null(mine);
    }

    [Fact]
    public async Task GetMyWorkoutSeries_WhenTheTermIsOver_ReportsItExpired()
    {
        await SeedAssignedAsync(Now.AddDays(-WorkoutSeriesAssignment.TermInDays));

        var mine = await MySeries(Athlete).ExecuteAsync(CancellationToken.None);

        Assert.NotNull(mine);
        Assert.True(mine!.IsExpired);
        Assert.Equal(0, mine.DaysRemaining);
    }

    [Fact]
    public async Task ListAthleteWorkouts_ReturnsTheMeasuredHistory()
    {
        _history.Seed(
            _athlete,
            new WorkoutSessionDto(Guid.NewGuid(), Today, Now, Now.AddMinutes(30), 1800, null, []));

        var workouts = await History(StubCurrentUser.PersonalTrainer(_trainer)).ExecuteAsync(
            new ListAthleteWorkoutsCommand(_athlete.Value),
            CancellationToken.None);

        var only = Assert.Single(workouts);
        Assert.Equal(1800, only.DurationSeconds);
    }

    private StubCurrentUser Athlete => StubCurrentUser.Athlete(_athlete);

    private InMemoryWorkoutSeriesQueries Queries => new(_series, _assignments, _exercises, _muscleGroups, _sessions);

    private IStartWorkout Start(StubCurrentUser user) =>
        new StartWorkout(user, _series, _assignments, _sessions, _ids, _clock);

    private ILogPerformedSet Log(StubCurrentUser user) => new LogPerformedSet(user, _series, _sessions, _clock);

    private IFinishWorkout Finish(StubCurrentUser user) => new FinishWorkout(user, _sessions, _clock);

    private ICancelWorkout Cancel(StubCurrentUser user) => new CancelWorkout(user, _sessions);

    private IGetMyWorkoutSeries MySeries(StubCurrentUser user) => new GetMyWorkoutSeries(user, Queries, _clock);

    private IListAthleteWorkouts History(StubCurrentUser user) => new ListAthleteWorkouts(user, _history);

    private async Task<StartWorkoutResult> StartTodayAsync()
    {
        var (_, workout) = await SeedAssignedAsync();

        return await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);
    }

    private async Task<(WorkoutSeries Series, WorkoutSeriesWorkout Workout)> SeedAssignedAsync(
        DateTimeOffset? assignedAt = null)
    {
        var series = await SeedSeriesAsync(_trainer, "Hipertrofia A/B/C");
        var workout = series.Workouts[0];

        var assignment = WorkoutSeriesAssignment.Assign(
            new WorkoutSeriesAssignmentId(_ids.NewId()),
            series.Id,
            _athlete,
            _trainer,
            assignedAt ?? Now);

        await _assignments.SaveAsync(assignment, CancellationToken.None);

        return (series, workout);
    }

    private async Task<WorkoutSeries> SeedSeriesAsync(UserId trainer, string name)
    {
        var series = WorkoutSeries.Create(new WorkoutSeriesId(_ids.NewId()), new WorkoutSeriesName(name), trainer);

        series.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Peito e Tríceps"),
            [new PrescribedExercise(_bench, 3, 12, 40m, PrescribedExercise.DefaultRestSeconds)]);

        await _series.SaveAsync(series, CancellationToken.None);

        return series;
    }
}

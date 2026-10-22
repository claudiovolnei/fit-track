using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

/// <summary>
/// The cycle of a series: the personal trainer closes it early and the athlete is left without a
/// series, so the next one can be created when the time comes - carrying the sheet of the cycle
/// that just ended.
/// </summary>
public sealed class SeriesCycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkoutSeriesRepository _series = new();
    private readonly InMemoryWorkoutSeriesAssignmentRepository _assignments = new();
    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryUserSessionQueries _history = new();
    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _otherTrainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly ExerciseId _bench = ExerciseId.New();

    public SeriesCycleTests()
    {
        _muscleGroups.SaveAsync(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        _exercises.SaveAsync(Exercise.Create(_bench, new ExerciseName("Bench Press"), _chest), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
        _links.SeedLink(_trainer.Value, _athlete.Value);
    }

    [Fact]
    public async Task EndWorkoutSeries_AsTrainer_ClosesTheCycleAndLeavesTheAthleteWithoutASeries()
    {
        var series = await SeedSeriesAsync();

        await End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None);

        var assignment = Assert.Single(_assignments.All);
        Assert.False(assignment.IsOpen);
        Assert.Equal(Now, assignment.ClosedAt);
        Assert.Equal(series.Id, assignment.SeriesId);
    }

    [Fact]
    public async Task EndWorkoutSeries_LeavesTheAthleteWithoutASeriesToRead()
    {
        await SeedSeriesAsync();

        await End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None);

        var mine = await Queries.GetForAthleteAsync(
            _athlete,
            DateOnly.FromDateTime(Now.UtcDateTime),
            Now,
            CancellationToken.None);

        Assert.Null(mine);
    }

    [Fact]
    public async Task EndWorkoutSeries_OfAnAthleteOnAnotherRoster_Fails()
    {
        await SeedSeriesAsync();
        _links.SeedLink(_otherTrainer.Value, _athlete.Value);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None));

        Assert.True(Assert.Single(_assignments.All).IsOpen);
    }

    [Fact]
    public async Task EndWorkoutSeries_WithoutASeries_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None));
    }

    [Fact]
    public async Task EndWorkoutSeries_AsAthlete_Fails()
    {
        await SeedSeriesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            End(Athlete).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None));
    }

    [Fact]
    public async Task EndWorkoutSeries_WithAWorkoutRunning_Fails()
    {
        await SeedSeriesAsync();
        _history.Seed(_athlete, RunningSession());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None));

        Assert.Equal("workout_series.assignment.running", exception.Code);
        Assert.True(Assert.Single(_assignments.All).IsOpen);
    }

    [Fact]
    public async Task EndWorkoutSeries_Twice_KeepsTheFirstClosing()
    {
        await SeedSeriesAsync();

        await End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None);
        await End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None);

        var assignment = Assert.Single(_assignments.All);
        Assert.Equal(Now, assignment.ClosedAt);
    }

    [Fact]
    public async Task CreateWorkoutSeries_CopyingThePreviousSheet_CarriesTheWorkoutsOver()
    {
        var previous = await SeedSeriesAsync();
        previous.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Peito"),
            [new PrescribedExercise(_bench, 3, 12, 40m, 90)]);
        await _series.SaveAsync(previous, CancellationToken.None);

        var created = await Create(Trainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Hipertrofia fase 2", _athlete.Value, CopyPreviousWorkouts: true),
            CancellationToken.None);

        var workout = Assert.Single(created.Workouts);
        Assert.Equal("Peito", workout.Name);
        var exercise = Assert.Single(workout.Exercises);
        Assert.Equal(_bench.Value, exercise.ExerciseId);
        Assert.Equal(3, exercise.Sets);
        Assert.Equal(12, exercise.Repetitions);
        Assert.Equal(40m, exercise.LoadKilograms);
    }

    [Fact]
    public async Task CreateWorkoutSeries_WithoutCopying_StartsEmpty()
    {
        var previous = await SeedSeriesAsync();
        previous.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Peito"),
            [new PrescribedExercise(_bench, 3, 12, 40m, 90)]);
        await _series.SaveAsync(previous, CancellationToken.None);

        var created = await Create(Trainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Hipertrofia fase 2", _athlete.Value),
            CancellationToken.None);

        Assert.Empty(created.Workouts);
    }

    [Fact]
    public async Task CreateWorkoutSeries_AfterTheCycleWasClosed_StartsFromTheSheetOfTheEndedCycle()
    {
        var previous = await SeedSeriesAsync();
        previous.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Peito"),
            [new PrescribedExercise(_bench, 3, 12, 40m, 90)]);
        await _series.SaveAsync(previous, CancellationToken.None);

        await End(Trainer).ExecuteAsync(new EndWorkoutSeriesCommand(_athlete.Value), CancellationToken.None);

        var created = await Create(Trainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Hipertrofia fase 2", _athlete.Value, CopyPreviousWorkouts: true),
            CancellationToken.None);

        Assert.Single(created.Workouts);
        Assert.True(Assert.Single(_assignments.All).IsOpen);
    }

    private StubCurrentUser Trainer => StubCurrentUser.PersonalTrainer(_trainer);

    private StubCurrentUser Athlete => StubCurrentUser.Athlete(_athlete);

    private InMemoryWorkoutSeriesQueries Queries => new(_series, _assignments, _exercises, _muscleGroups, _sessions);

    private ICreateWorkoutSeries Create(StubCurrentUser user) =>
        new CreateWorkoutSeries(user, _series, Queries, _athletes, _links, _assignments, _clock, _ids);

    private IEndWorkoutSeries End(StubCurrentUser user) =>
        new EndWorkoutSeries(user, _links, _assignments, _history, _clock);

    private async Task<WorkoutSeries> SeedSeriesAsync()
    {
        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_ids.NewId()),
            new WorkoutSeriesName("Hipertrofia A/B/C"),
            _trainer,
            _athlete);

        await _series.SaveAsync(series, CancellationToken.None);

        var assignment = WorkoutSeriesAssignment.Assign(
            new WorkoutSeriesAssignmentId(_ids.NewId()),
            series.Id,
            _athlete,
            _trainer,
            Now);

        await _assignments.SaveAsync(assignment, CancellationToken.None);

        return series;
    }

    private WorkoutSessionDto RunningSession() =>
        new(
            Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            Now,
            FinishedAt: null,
            DurationSeconds: 0,
            WorkoutSeriesWorkoutId: null,
            Sets: [],
            Volume: null);
}

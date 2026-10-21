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

public sealed class WorkoutSeriesManagementTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkoutSeriesRepository _series = new();
    private readonly InMemoryWorkoutSeriesAssignmentRepository _assignments = new();
    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
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
    private readonly ExerciseId _row = ExerciseId.New();

    public WorkoutSeriesManagementTests()
    {
        SaveMuscleGroup(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")));
        SaveExercise(Exercise.Create(_bench, new ExerciseName("Bench Press"), _chest));
        SaveExercise(Exercise.Create(_row, new ExerciseName("Barbell Row"), _chest));
        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");

        // The athlete starts on the trainer's roster: the link screen is tested on its own.
        _links.SeedLink(_trainer.Value, _athlete.Value);
    }

    [Fact]
    public async Task CreateWorkoutSeries_AsPersonalTrainer_SavesAndReturnsIt()
    {
        var created = await Create(Trainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Hipertrofia A/B/C", _athlete.Value),
            CancellationToken.None);

        Assert.Equal("Hipertrofia A/B/C", created.Name);
        Assert.Equal(_trainer.Value, created.TrainerId);
        Assert.Empty(created.Workouts);
        Assert.Contains(_series.All, saved => saved.Id.Value == created.Id);
    }

    [Fact]
    public async Task CreateWorkoutSeries_AsAthlete_Fails()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Create(Athlete).ExecuteAsync(new CreateWorkoutSeriesCommand("Hipertrofia", _athlete.Value), CancellationToken.None));

        Assert.Empty(_series.All);
    }

    [Fact]
    public async Task CreateWorkoutSeries_RepeatedNameForTheSameTrainer_Fails()
    {
        await SeedSeriesAsync(_trainer, "Hipertrofia A/B/C");

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Create(Trainer).ExecuteAsync(new CreateWorkoutSeriesCommand("  Hipertrofia A/B/C  ", _athlete.Value), CancellationToken.None));

        Assert.Equal("workout_series.name.duplicate", exception.Code);
        Assert.Single(_series.All);
    }

    [Fact]
    public async Task CreateWorkoutSeries_SameNameForAnotherTrainer_IsAllowed()
    {
        await SeedSeriesAsync(_trainer, "Hipertrofia A/B/C");
        _links.SeedLink(_otherTrainer.Value, _athlete.Value);

        var created = await Create(OtherTrainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Hipertrofia A/B/C", _athlete.Value),
            CancellationToken.None);

        Assert.Equal(_otherTrainer.Value, created.TrainerId);
        Assert.Equal(2, _series.All.Count);
    }

    [Fact]
    public async Task RenameWorkoutSeries_KeepingItsOwnName_IsAllowed()
    {
        var series = await SeedSeriesAsync(_trainer, "Hipertrofia A/B/C");

        var renamed = await Rename(Trainer).ExecuteAsync(
            new RenameWorkoutSeriesCommand(series.Id.Value, "Hipertrofia A/B/C"),
            CancellationToken.None);

        Assert.Equal("Hipertrofia A/B/C", renamed.Name);
    }

    [Fact]
    public async Task RenameWorkoutSeries_OfAnotherTrainer_Fails()
    {
        var series = await SeedSeriesAsync(_trainer, "Hipertrofia");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Rename(OtherTrainer).ExecuteAsync(
                new RenameWorkoutSeriesCommand(series.Id.Value, "Força"),
                CancellationToken.None));
    }

    [Fact]
    public async Task RenameWorkoutSeries_UnknownSeries_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Rename(Trainer).ExecuteAsync(
                new RenameWorkoutSeriesCommand(Guid.NewGuid(), "Força"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AddSeriesWorkout_AddsTheWorkoutWithNamesResolved()
    {
        var series = await SeedSeriesAsync(_trainer);

        var created = await Add(Trainer).ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, "Peito e Tríceps", [Input(_bench, 4, 10, 50m)]),
            CancellationToken.None);

        Assert.Equal(1, created.Position);
        Assert.Equal("A", created.Label);
        Assert.Equal("Peito e Tríceps", created.Name);
        Assert.Null(created.Today);

        var exercise = Assert.Single(created.Exercises);
        Assert.Equal(_bench.Value, exercise.ExerciseId);
        Assert.Equal("Bench Press", exercise.ExerciseName);
        Assert.Equal("Chest", exercise.MuscleGroupName);
        Assert.Equal(4, exercise.Sets);
        Assert.Equal(10, exercise.Repetitions);
        Assert.Equal(50m, exercise.LoadKilograms);
    }

    [Fact]
    public async Task AddSeriesWorkout_WithoutRest_CountsTheDefaultRest()
    {
        var series = await SeedSeriesAsync(_trainer);

        var created = await Add(Trainer).ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench)]),
            CancellationToken.None);

        var exercise = Assert.Single(created.Exercises);
        Assert.Equal(PrescribedExercise.DefaultRestSeconds, exercise.RestSeconds);
    }

    [Fact]
    public async Task AddSeriesWorkout_WithRest_KeepsThePrescribedRest()
    {
        var series = await SeedSeriesAsync(_trainer);

        var created = await Add(Trainer).ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench, restSeconds: 90)]),
            CancellationToken.None);

        var exercise = Assert.Single(created.Exercises);
        Assert.Equal(90, exercise.RestSeconds);
    }

    [Fact]
    public async Task AddSeriesWorkout_WithRestOutOfRange_IsRefused()
    {
        var series = await SeedSeriesAsync(_trainer);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Add(Trainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench, restSeconds: 601)]),
                CancellationToken.None));

        Assert.Equal("workout_series.exercise.prescription.invalid", exception.Code);
        Assert.Empty(series.Workouts);
    }

    [Fact]
    public async Task AddSeriesWorkout_OfAnotherTrainer_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Add(OtherTrainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench)]),
                CancellationToken.None));

        Assert.Empty(series.Workouts);
    }

    [Fact]
    public async Task AddSeriesWorkout_UnknownExercise_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Add(Trainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(ExerciseId.New())]),
                CancellationToken.None));

        Assert.Empty(series.Workouts);
    }

    [Fact]
    public async Task AddSeriesWorkout_OutOfRangePrescription_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Add(Trainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench, sets: 0)]),
                CancellationToken.None));

        Assert.Equal("workout_series.exercise.prescription.invalid", exception.Code);
        Assert.Empty(series.Workouts);
    }

    [Fact]
    public async Task AddSeriesWorkout_BeyondFive_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        for (var index = 1; index <= WorkoutSeries.MaxWorkouts; index++)
        {
            await Add(Trainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, $"Treino {index}", [Input(_bench)]),
                CancellationToken.None);
        }

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Add(Trainer).ExecuteAsync(
                new AddSeriesWorkoutCommand(series.Id.Value, "Treino 6", [Input(_row)]),
                CancellationToken.None));

        Assert.Equal("workout_series.workout.limit_reached", exception.Code);
        Assert.Equal(WorkoutSeries.MaxWorkouts, series.Workouts.Count);
    }

    [Fact]
    public async Task AddSeriesWorkout_LabelsFollowTheAddOrder()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Add(Trainer).ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, "Peito", [Input(_bench)]),
            CancellationToken.None);

        var second = await Add(Trainer).ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, "Costas", [Input(_row)]),
            CancellationToken.None);

        Assert.Equal(2, second.Position);
        Assert.Equal("B", second.Label);
    }

    [Fact]
    public async Task ChangeSeriesWorkout_ReplacesThePrescription()
    {
        var series = await SeedSeriesAsync(_trainer);
        var workout = await AddWorkoutAsync(series, "Peito", _bench);

        var changed = await Change(Trainer).ExecuteAsync(
            new ChangeSeriesWorkoutCommand(series.Id.Value, workout.Id.Value, "Costas", [Input(_row, 5, 5, 80m)]),
            CancellationToken.None);

        Assert.Equal("Costas", changed.Name);
        var exercise = Assert.Single(changed.Exercises);
        Assert.Equal(_row.Value, exercise.ExerciseId);
        Assert.Equal(5, exercise.Sets);
        Assert.Equal(5, exercise.Repetitions);
        Assert.Equal(80m, exercise.LoadKilograms);
    }

    [Fact]
    public async Task ChangeSeriesWorkout_UnknownWorkout_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Change(Trainer).ExecuteAsync(
                new ChangeSeriesWorkoutCommand(series.Id.Value, Guid.NewGuid(), "Peito", [Input(_bench)]),
                CancellationToken.None));

        Assert.Equal("workout_series.workout.not_found", exception.Code);
    }

    [Fact]
    public async Task RemoveSeriesWorkout_RenumbersTheLabels()
    {
        var series = await SeedSeriesAsync(_trainer);
        var first = await AddWorkoutAsync(series, "Peito", _bench);
        var second = await AddWorkoutAsync(series, "Costas", _row);

        await Remove(Trainer).ExecuteAsync(
            new RemoveSeriesWorkoutCommand(series.Id.Value, first.Id.Value),
            CancellationToken.None);

        var remaining = Assert.Single(series.Workouts);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(1, remaining.Position);
        Assert.Equal("A", remaining.Label);
    }

    [Fact]
    public async Task ListWorkoutSeries_ReturnsOnlyTheTrainersSeries()
    {
        await SeedSeriesAsync(_trainer, "Hipertrofia A/B/C");
        await SeedSeriesAsync(_otherTrainer, "Força A/B");

        var mine = await List(Trainer).ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(mine);
        Assert.Equal("Hipertrofia A/B/C", only.Name);
    }

    [Fact]
    public async Task ListWorkoutSeries_AsAthlete_Fails()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => List(Athlete).ExecuteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetWorkoutSeries_OfAnotherTrainer_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Get(OtherTrainer).ExecuteAsync(series.Id.Value, CancellationToken.None));
    }

    [Fact]
    public async Task ListAthletes_ShowsTheAssignedSeriesAndTheDaysRemaining()
    {
        var series = await SeedSeriesAsync(_trainer);
        _athletes.Link(_trainer.Value, _athlete.Value);
        _athletes.AssignSeries(_athlete.Value, series.Id.Value, series.Name.Value, Now.AddDays(WorkoutSeriesAssignment.TermInDays));

        var athletes = await ListAthletes(Trainer).ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(athletes);
        Assert.Equal(_athlete.Value, only.UserId);
        Assert.Equal(series.Id.Value, only.WorkoutSeriesId);
        Assert.Equal(series.Name.Value, only.WorkoutSeriesName);
        Assert.Equal(WorkoutSeriesAssignment.TermInDays, only.DaysRemaining);
    }

    [Fact]
    public async Task ListAthletes_WithoutASeries_LeavesTheFieldsEmpty()
    {
        _athletes.Link(_trainer.Value, _athlete.Value);

        var athletes = await ListAthletes(Trainer).ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(athletes);
        Assert.Null(only.WorkoutSeriesId);
        Assert.Null(only.WorkoutSeriesName);
        Assert.Equal(0, only.DaysRemaining);
    }

    private StubCurrentUser Trainer => StubCurrentUser.PersonalTrainer(_trainer);

    private StubCurrentUser OtherTrainer => StubCurrentUser.PersonalTrainer(_otherTrainer);

    private StubCurrentUser Athlete => StubCurrentUser.Athlete(_athlete);

    private InMemoryWorkoutSeriesQueries Queries => new(_series, _assignments, _exercises, _muscleGroups, _sessions);

    private ICreateWorkoutSeries Create(StubCurrentUser user) => new CreateWorkoutSeries(user, _series, Queries, _athletes, _links, _assignments, _clock, _ids);

    private IRenameWorkoutSeries Rename(StubCurrentUser user) => new RenameWorkoutSeries(user, _series, Queries);

    private IListWorkoutSeries List(StubCurrentUser user) => new ListWorkoutSeries(user, Queries);

    private IGetWorkoutSeries Get(StubCurrentUser user) => new GetWorkoutSeries(user, Queries);

    private IAddSeriesWorkout Add(StubCurrentUser user) => new AddSeriesWorkout(user, _series, _exercises, Queries, _ids);

    private IChangeSeriesWorkout Change(StubCurrentUser user) => new ChangeSeriesWorkout(user, _series, _exercises, Queries);

    private IRemoveSeriesWorkout Remove(StubCurrentUser user) => new RemoveSeriesWorkout(user, _series);

    private IListAthletes ListAthletes(StubCurrentUser user) => new ListAthletes(user, _athletes, _clock);

    private async Task<WorkoutSeries> SeedSeriesAsync(UserId trainer, string name = "Hipertrofia A/B/C")
    {
        var series = WorkoutSeries.Create(new WorkoutSeriesId(_ids.NewId()), new WorkoutSeriesName(name), trainer);

        await _series.SaveAsync(series, CancellationToken.None);

        return series;
    }

    private async Task<WorkoutSeriesWorkout> AddWorkoutAsync(WorkoutSeries series, string name, ExerciseId exercise)
    {
        var workout = new AddSeriesWorkout(Trainer, _series, _exercises, Queries, _ids);

        var created = await workout.ExecuteAsync(
            new AddSeriesWorkoutCommand(series.Id.Value, name, [Input(exercise)]),
            CancellationToken.None);

        return series.FindWorkout(new WorkoutSeriesWorkoutId(created.Id));
    }

    private void SaveMuscleGroup(MuscleGroup muscleGroup) =>
        _muscleGroups.SaveAsync(muscleGroup, CancellationToken.None).GetAwaiter().GetResult();

    private void SaveExercise(Exercise exercise) =>
        _exercises.SaveAsync(exercise, CancellationToken.None).GetAwaiter().GetResult();

    private static PrescribedExerciseInput Input(
        ExerciseId exercise,
        int sets = 3,
        int repetitions = 12,
        decimal? load = 40m,
        int? restSeconds = null) =>
        new(exercise.Value, sets, repetitions, load, restSeconds);
}

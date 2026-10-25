using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.TestCases;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

/// <summary>
/// Drives the refusals of the series slice through the use cases over the in-memory fakes. The
/// integration suite drives the very same table over HTTP against PostgreSQL, so the two suites keep the
/// fake and the adapter answering an invalid input the same way instead of drifting apart silently.
/// </summary>
public sealed class SeriesRefusalTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkoutSeriesRepository _series = new();
    private readonly InMemoryWorkoutSeriesAssignmentRepository _assignments = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly InMemoryAccountPermissions _permissions = new();
    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly ExerciseId _bench = ExerciseId.New();

    public SeriesRefusalTests()
    {
        SaveMuscleGroup(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")));
        SaveExercise(Exercise.Create(_bench, new ExerciseName("Bench Press"), _chest));
        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
        _links.SeedLink(_trainer.Value, _athlete.Value);
    }

    [Theory]
    [MemberData(nameof(SeriesRefusals.Refusals), MemberType = typeof(SeriesRefusals))]
    public async Task EveryRefusal_ComesBackWithTheAgreedCode(string scenario, string code)
    {
        var refused = await RefuseAsync(scenario);

        Assert.Equal(code, refused);
    }

    /// <summary>
    /// The code the fakes answer for one scenario. Each arm arranges what the rule needs and stops at the
    /// intention that breaks it, so the assertion is about the refusal and not about the arrangement.
    /// </summary>
    private async Task<string> RefuseAsync(string scenario) => scenario switch
    {
        SeriesRefusals.SeriesNameRequired => await CodeOfAsync(() => CreateAsync(" ")),

        SeriesRefusals.SeriesNameDuplicate => await CodeOfAsync(async () =>
        {
            await CreateAsync("Hipertrofia A/B/C");
            await CreateAsync("Hipertrofia A/B/C");
        }),

        SeriesRefusals.WorkoutNameRequired => await CodeOfAsync(() =>
            AddToANewSeriesAsync(" ", [Prescription(_bench)])),

        SeriesRefusals.WorkoutPrescriptionEmpty => await CodeOfAsync(() =>
            AddToANewSeriesAsync("Peito e Tríceps", [])),

        SeriesRefusals.WorkoutPrescriptionDuplicateExercise => await CodeOfAsync(() =>
            AddToANewSeriesAsync("Peito e Tríceps", [Prescription(_bench), Prescription(_bench)])),

        SeriesRefusals.WorkoutPrescriptionOutOfRange => await CodeOfAsync(() =>
            AddToANewSeriesAsync("Peito e Tríceps", [Prescription(_bench, sets: 0)])),

        SeriesRefusals.WorkoutRestOutOfRange => await CodeOfAsync(() =>
            AddToANewSeriesAsync("Peito e Tríceps", [Prescription(_bench, restSeconds: 601)])),

        SeriesRefusals.WorkoutLimitReached => await CodeOfAsync(async () =>
        {
            var seriesId = await CreateAsync("Hipertrofia A/B/C");

            for (var index = 1; index <= WorkoutSeries.MaxWorkouts; index++)
            {
                await AddWorkoutAsync(seriesId, $"Treino {index}", [Prescription(_bench)]);
            }

            await AddWorkoutAsync(seriesId, "Treino 6", [Prescription(_bench)]);
        }),

        SeriesRefusals.WorkoutRemovedTwice => await CodeOfAsync(async () =>
        {
            var seriesId = await CreateAsync("Hipertrofia A/B/C");
            var workout = await AddWorkoutAsync(seriesId, "Peito e Tríceps", [Prescription(_bench)]);

            await RemoveWorkoutAsync(seriesId, workout.Id);
            await RemoveWorkoutAsync(seriesId, workout.Id);
        }),

        SeriesRefusals.AssignmentExpired => await CodeOfAsync(async () =>
        {
            var workoutId = await SeedAssignedAsync(Now.AddDays(-(WorkoutSeriesAssignment.TermInDays + 1)));

            await StartAsync(workoutId);
        }),

        SeriesRefusals.SetOutsidePrescription => await CodeOfAsync(async () =>
        {
            var started = await StartAsync(await SeedAssignedAsync());

            await FinishAsync(started.Session.Id, ExerciseId.New());
        }),

        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown series refusal."),
    };

    private static async Task<string> CodeOfAsync(Func<Task> intention) =>
        (await Assert.ThrowsAsync<DomainException>(intention)).Code;

    /// <summary>A series created through the use case, the way the trainer's screen creates one.</summary>
    private async Task<Guid> CreateAsync(string name) =>
        (await Create.ExecuteAsync(new CreateWorkoutSeriesCommand(name, _athlete.Value), CancellationToken.None)).Id;

    private Task<WorkoutSeriesWorkoutDto> AddWorkoutAsync(
        Guid seriesId,
        string name,
        IReadOnlyList<PrescribedExerciseInput> exercises) =>
        Add.ExecuteAsync(new AddSeriesWorkoutCommand(seriesId, name, exercises), CancellationToken.None);

    /// <summary>The add intention pointed at a brand-new series, so an arm states only the broken input.</summary>
    private async Task AddToANewSeriesAsync(string name, IReadOnlyList<PrescribedExerciseInput> exercises) =>
        await AddWorkoutAsync(await CreateAsync("Hipertrofia A/B/C"), name, exercises);

    private Task RemoveWorkoutAsync(Guid seriesId, Guid workoutId) =>
        Remove.ExecuteAsync(new RemoveSeriesWorkoutCommand(seriesId, workoutId), CancellationToken.None);

    private Task<StartWorkoutResult> StartAsync(Guid workoutId) =>
        Start.ExecuteAsync(new StartWorkoutCommand(workoutId), CancellationToken.None);

    private Task<WorkoutSessionDto> FinishAsync(Guid sessionId, ExerciseId exercise) =>
        Finish.ExecuteAsync(
            new FinishWorkoutCommand(
                sessionId,
                WorkoutIntensity.Moderado,
                null,
                [new PerformedExerciseInput(exercise.Value, [new PerformedSetInput(10, 40m)])]),
            CancellationToken.None);

    /// <summary>
    /// A series with one prescribed workout, assigned to the athlete, entirely through the use cases: the
    /// fakes hold exactly what the API would have persisted. A start instant older than the term is
    /// written back through the assignment, the same way the term is derived from it on every read.
    /// </summary>
    private async Task<Guid> SeedAssignedAsync(DateTimeOffset? assignedAt = null)
    {
        var seriesId = await CreateAsync("Hipertrofia A/B/C");
        var workout = await AddWorkoutAsync(seriesId, "Peito e Tríceps", [Prescription(_bench)]);

        if (assignedAt is { } start)
        {
            var assignment = (await _assignments.GetByUserIdAsync(_athlete, CancellationToken.None))!;

            await _assignments.SaveAsync(
                WorkoutSeriesAssignment.Assign(
                    assignment.Id,
                    assignment.SeriesId,
                    assignment.UserId,
                    assignment.TrainerId,
                    start),
                CancellationToken.None);
        }

        return workout.Id;
    }

    private StubCurrentUser Trainer => StubCurrentUser.PersonalTrainer(_trainer);

    private StubCurrentUser Athlete => StubCurrentUser.Athlete(_athlete);

    private IQueryWorkoutSeries Queries => new InMemoryWorkoutSeriesQueries(
        _series,
        _assignments,
        _exercises,
        _muscleGroups,
        _sessions);

    private ICreateWorkoutSeries Create => new CreateWorkoutSeries(Trainer, _series, Queries, _athletes, _links, _assignments, _clock, _ids, _permissions);

    private IAddSeriesWorkout Add => new AddSeriesWorkout(Trainer, _series, _exercises, Queries, _ids, _permissions);

    private IRemoveSeriesWorkout Remove => new RemoveSeriesWorkout(Trainer, _series, _permissions);

    private IStartWorkout Start => new StartWorkout(Athlete, _series, _assignments, _sessions, _ids, _clock);

    private IFinishWorkout Finish => new FinishWorkout(Athlete, _series, _sessions, _clock);

    private void SaveMuscleGroup(MuscleGroup muscleGroup) =>
        _muscleGroups.SaveAsync(muscleGroup, CancellationToken.None).GetAwaiter().GetResult();

    private void SaveExercise(Exercise exercise) =>
        _exercises.SaveAsync(exercise, CancellationToken.None).GetAwaiter().GetResult();

    private static PrescribedExerciseInput Prescription(
        ExerciseId exercise,
        int sets = 3,
        int repetitions = 12,
        decimal? load = 40m,
        int? restSeconds = null) =>
        new(exercise.Value, sets, repetitions, load, restSeconds);
}

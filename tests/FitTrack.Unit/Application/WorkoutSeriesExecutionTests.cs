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
    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly InMemoryUserSessionQueries _history = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly ExerciseId _bench = ExerciseId.New();
    private readonly ExerciseId _row = ExerciseId.New();

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

        _exercises
            .SaveAsync(Exercise.Create(_row, new ExerciseName("Barbell Row"), _chest), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        // The athlete follows the trainer, so their history read passes the privacy guard.
        _links.SeedLink(_trainer.Value, _athlete.Value);
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
    public async Task StartWorkout_AnotherWorkoutOfTheSameDay_Fails()
    {
        var (_, workout) = await SeedAssignedAsync();
        var second = await SeedSecondWorkoutAsync();

        await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Start(Athlete).ExecuteAsync(
                new StartWorkoutCommand(second.Id.Value),
                CancellationToken.None));

        Assert.Equal("session.start.already_started_today", exception.Code);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task StartWorkout_AnotherWorkoutAfterFinishingTheDay_Fails()
    {
        var (_, workout) = await SeedAssignedAsync();
        var second = await SeedSecondWorkoutAsync();

        var started = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Start(Athlete).ExecuteAsync(
                new StartWorkoutCommand(second.Id.Value),
                CancellationToken.None));

        Assert.Equal("session.start.already_started_today", exception.Code);
    }

    [Fact]
    public async Task StartWorkout_TheSameWorkoutAfterFinishingTheDay_ResumesTheSession()
    {
        var (_, workout) = await SeedAssignedAsync();

        var started = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        var again = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        Assert.False(again.Started);
        Assert.Equal(started.Session.Id, again.Session.Id);
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
    public async Task FinishWorkout_RecordsTheSetsOfTheMarkedExercises()
    {
        var started = await StartTodayAsync();

        // The screen sends the whole workout at once: the exercise, the effort the athlete used and
        // the account of the session.
        var finished = await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(
                started.Session.Id,
                WorkoutIntensity.Intenso,
                "Senti o ombro.",
                [Performed(_bench, repetitions: 8, load: 80m)]),
            CancellationToken.None);

        Assert.NotNull(finished.FinishedAt);
        Assert.Equal(0, finished.DurationSeconds);
        Assert.Equal("intenso", finished.Intensity);
        Assert.Equal("Senti o ombro.", finished.Notes);
        Assert.Equal([_bench.Value], finished.CompletedExerciseIds);

        // Three sets come from the prescription; the repetitions and the load are the athlete's.
        Assert.Equal(3, finished.Sets.Count);
        Assert.All(finished.Sets, set =>
        {
            Assert.Equal(_bench.Value, set.ExerciseId);
            Assert.Equal(80m, set.LoadKilograms);
            Assert.Equal(8, set.Repetitions);
        });
    }

    [Fact]
    public async Task FinishWorkout_WithAnExerciseOutsideThePrescription_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    null,
                    [Performed(_bench), Performed(ExerciseId.New())]),
                CancellationToken.None));

        Assert.Equal("session.set.exercise.not_prescribed", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_OnSomebodyElsesWorkout_Fails()
    {
        var started = await StartTodayAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Finish(StubCurrentUser.Athlete(UserId.New())).ExecuteAsync(
                new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task FinishWorkout_ClosesTheWorkout()
    {
        var started = await StartTodayAsync();

        var finished = await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        Assert.NotNull(finished.FinishedAt);
        Assert.Equal(0, finished.DurationSeconds);
    }

    [Fact]
    public async Task FinishWorkout_WithoutAnIntensity_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(started.Session.Id, null, null, [Performed(_bench)]),
                CancellationToken.None));

        Assert.Equal("session.finish.intensity.invalid", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_WithALongNote_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    new string('a', 501),
                    [Performed(_bench)]),
                CancellationToken.None));

        Assert.Equal("workout_session.notes.too_long", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_WithAnExerciseStillPending_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, []),
                CancellationToken.None));

        Assert.Equal("session.finish.exercises.pending", exception.Code);
        Assert.Null(
            (await _sessions.GetByIdAsync(new WorkoutSessionId(started.Session.Id), CancellationToken.None))!.FinishedAt);
    }

    [Fact]
    public async Task FinishWorkout_WithOneExerciseStillPending_Fails()
    {
        // A prescription with two exercises: the athlete marked one of them, so the sheet is
        // incomplete and the workout cannot close. The empty list is only one shape of "pending" —
        // this is the shape the screen really sends.
        var (_, workout) = await SeedAssignedWithTwoExercisesAsync();

        var started = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    null,
                    [Performed(_bench)]),
                CancellationToken.None));

        Assert.Equal("session.finish.exercises.pending", exception.Code);

        // The session is still running: the refusal leaves no trace of a closed workout.
        Assert.Null(
            (await _sessions.GetByIdAsync(new WorkoutSessionId(started.Session.Id), CancellationToken.None))!.FinishedAt);
    }

    [Fact]
    public async Task FinishWorkout_WithANegativeLoad_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    null,
                    [Performed(_bench, load: -1m)]),
                CancellationToken.None));

        Assert.Equal("set.load.negative", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_WithMoreThanAHundredRepetitions_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    null,
                    [Performed(_bench, repetitions: 101)]),
                CancellationToken.None));

        Assert.Equal("set.repetitions.out_of_range", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_WithEveryExerciseTicked_Closes()
    {
        var started = await StartTodayAsync();

        var finished = await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        Assert.NotNull(finished.FinishedAt);
        Assert.Equal([_bench.Value], finished.CompletedExerciseIds);
    }

    [Fact]
    public async Task FinishWorkout_WithTheRepetitionsOfTheAthlete_UsesThem()
    {
        var started = await StartTodayAsync();

        var finished = await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(
                started.Session.Id,
                WorkoutIntensity.Moderado,
                null,
                [Performed(_bench, repetitions: 2)]),
            CancellationToken.None);

        Assert.All(finished.Sets, set => Assert.Equal(2, set.Repetitions));
    }

    [Fact]
    public async Task FinishWorkout_WithImpossibleRepetitions_Fails()
    {
        var started = await StartTodayAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(
                    started.Session.Id,
                    WorkoutIntensity.Moderado,
                    null,
                    [Performed(_bench, repetitions: 0)]),
                CancellationToken.None));

        Assert.Equal("set.repetitions.out_of_range", exception.Code);
    }

    [Fact]
    public async Task FinishWorkout_Twice_Fails()
    {
        var started = await StartTodayAsync();

        await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            Finish(Athlete).ExecuteAsync(
                new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
                CancellationToken.None));

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
        var (_, workout) = await SeedAssignedAsync();

        // A session that already holds sets is not erased: the wrong workout never disappears.
        var session = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            _athlete,
            new WorkoutDate(Today),
            Now,
            workout.Id);

        session.RegisterSet(_bench, new Load(80m), new Repetitions(12));

        await _sessions.SaveAsync(session, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCommandException>(() =>
            Cancel(Athlete).ExecuteAsync(new CancelWorkoutCommand(session.Id.Value), CancellationToken.None));

        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task CancelAthleteWorkout_TrainerFreesTheDay_AndTheAthleteStartsAnother()
    {
        // The athlete started A earlier and cannot start B on the same day...
        var started = await StartTodayAsync();
        var workoutB = await SeedSecondWorkoutAsync();

        var blocked = await Assert.ThrowsAsync<ConflictException>(() =>
            Start(Athlete).ExecuteAsync(new StartWorkoutCommand(workoutB.Id.Value), CancellationToken.None));

        Assert.Equal("session.start.already_started_today", blocked.Code);

        // ...until the trainer cancels the running workout, freeing the day.
        await CancelAthlete(StubCurrentUser.PersonalTrainer(_trainer)).ExecuteAsync(
            new CancelAthleteWorkoutCommand(_athlete.Value, started.Session.Id),
            CancellationToken.None);

        Assert.Empty(_sessions.All);

        var again = await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workoutB.Id.Value),
            CancellationToken.None);

        Assert.True(again.Started);
    }

    [Fact]
    public async Task CancelAthleteWorkout_AnAthleteCaller_IsForbidden()
    {
        var started = await StartTodayAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CancelAthlete(Athlete).ExecuteAsync(
                new CancelAthleteWorkoutCommand(_athlete.Value, started.Session.Id),
                CancellationToken.None));

        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task CancelAthleteWorkout_AnAthleteOffTheRoster_IsNotFound()
    {
        var stranger = UserId.New();
        var session = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            stranger,
            new WorkoutDate(Today),
            Now,
            WorkoutSeriesWorkoutId.New());

        await _sessions.SaveAsync(session, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            CancelAthlete(StubCurrentUser.PersonalTrainer(_trainer)).ExecuteAsync(
                new CancelAthleteWorkoutCommand(stranger.Value, session.Id.Value),
                CancellationToken.None));

        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task CancelAthleteWorkout_AnAdminCancelsAnyAthletesWorkout()
    {
        var stranger = UserId.New();
        var session = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            stranger,
            new WorkoutDate(Today),
            Now,
            WorkoutSeriesWorkoutId.New());

        await _sessions.SaveAsync(session, CancellationToken.None);

        await CancelAthlete(StubCurrentUser.Admin(_trainer)).ExecuteAsync(
            new CancelAthleteWorkoutCommand(stranger.Value, session.Id.Value),
            CancellationToken.None);

        Assert.Empty(_sessions.All);
    }

    [Fact]
    public async Task CancelAthleteWorkout_AFinishedWorkout_IsRefused()
    {
        var started = await StartTodayAsync();

        await Finish(Athlete).ExecuteAsync(
            new FinishWorkoutCommand(started.Session.Id, WorkoutIntensity.Moderado, null, [Performed(_bench)]),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CancelAthlete(StubCurrentUser.PersonalTrainer(_trainer)).ExecuteAsync(
                new CancelAthleteWorkoutCommand(_athlete.Value, started.Session.Id),
                CancellationToken.None));

        Assert.Equal("session.cancel.not_running", exception.Code);
        Assert.Single(_sessions.All);
    }

    [Fact]
    public async Task CancelAthleteWorkout_AWorkoutWithSets_IsRefused()
    {
        var (_, workout) = await SeedAssignedAsync();
        var session = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            _athlete,
            new WorkoutDate(Today),
            Now,
            workout.Id);

        session.RegisterSet(_bench, new Load(80m), new Repetitions(12));
        await _sessions.SaveAsync(session, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CancelAthlete(StubCurrentUser.PersonalTrainer(_trainer)).ExecuteAsync(
                new CancelAthleteWorkoutCommand(_athlete.Value, session.Id.Value),
                CancellationToken.None));

        Assert.Equal("session.cancel.has_sets", exception.Code);
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

    private IFinishWorkout Finish(StubCurrentUser user) => new FinishWorkout(user, _series, _sessions, _clock);

    private ICancelWorkout Cancel(StubCurrentUser user) => new CancelWorkout(user, _sessions);

    private ICancelAthleteWorkout CancelAthlete(StubCurrentUser user) =>
        new CancelAthleteWorkout(user, _links, _sessions);

    private IGetMyWorkoutSeries MySeries(StubCurrentUser user) => new GetMyWorkoutSeries(user, Queries, _clock);

    private IListAthleteWorkouts History(StubCurrentUser user) => new ListAthleteWorkouts(user, _links, _history);

    private async Task<StartWorkoutResult> StartTodayAsync()
    {
        var (_, workout) = await SeedAssignedAsync();

        return await Start(Athlete).ExecuteAsync(
            new StartWorkoutCommand(workout.Id.Value),
            CancellationToken.None);
    }

    /// <summary>
    /// What the screen sends for one marked exercise: the repetitions and the load the athlete used.
    /// </summary>
    private static PerformedExerciseInput Performed(ExerciseId exercise, int repetitions = 12, decimal load = 40m) =>
        new(exercise.Value, [new PerformedSetInput(repetitions, load), new PerformedSetInput(repetitions, load), new PerformedSetInput(repetitions, load)]);

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

    /// <summary>
    /// A series whose workout prescribes two exercises, so the sheet can be one mark short of the
    /// whole prescription.
    /// </summary>
    private async Task<(WorkoutSeries Series, WorkoutSeriesWorkout Workout)> SeedAssignedWithTwoExercisesAsync()
    {
        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_ids.NewId()),
            new WorkoutSeriesName("Peito e Costas"),
            _trainer);

        var workout = series.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Peito e Costas"),
            [
                new PrescribedExercise(_bench, 3, 12, 40m, PrescribedExercise.DefaultRestSeconds),
                new PrescribedExercise(_row, 4, 10, 20m, PrescribedExercise.DefaultRestSeconds),
            ]);

        await _series.SaveAsync(series, CancellationToken.None);

        var assignment = WorkoutSeriesAssignment.Assign(
            new WorkoutSeriesAssignmentId(_ids.NewId()),
            series.Id,
            _athlete,
            _trainer,
            Now);

        await _assignments.SaveAsync(assignment, CancellationToken.None);

        return (series, workout);
    }

    /// <summary>A second workout (B) on the series the athlete follows, so the day can hold two.</summary>
    private async Task<WorkoutSeriesWorkout> SeedSecondWorkoutAsync()
    {
        var assignment = _assignments.All.Single(candidate => candidate.UserId == _athlete);
        var series = _series.All.Single(candidate => candidate.Id == assignment.SeriesId);

        var workout = series.AddWorkout(
            new WorkoutSeriesWorkoutId(_ids.NewId()),
            new WorkoutName("Costas"),
            [new PrescribedExercise(_bench, 3, 12, 40m, PrescribedExercise.DefaultRestSeconds)]);

        await _series.SaveAsync(series, CancellationToken.None);

        return workout;
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

using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The athlete's workout end to end: the workout comes from the series the trainer assigned, the timer
/// starts on the server, and the athlete closes the workout in one request — the exercises they marked,
/// the level they gave the session and what they wrote about it — which freezes the duration the
/// history then shows.
/// </summary>
public sealed class SessionFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task Athlete_StartsAndFinishesTheWorkoutOfTheSeries()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Peito");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 10, 50m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));

        Assert.Equal(HttpStatusCode.Created, started.StatusCode);

        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        Assert.Equal(workoutId, session.WorkoutSeriesWorkoutId);
        Assert.Null(session.FinishedAt);
        Assert.Equal(0, session.DurationSeconds);

        // Starting the same workout again resumes the one running instead of opening a second session.
        var again = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(
            session.Id,
            (await again.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!.Id);

        // The sheet shows the timer running, and reloading does not restart it.
        var running = (await athlete.Client.ReadMySeriesAsync())!;
        var today = Assert.Single(running.Series.Workouts).Today;

        Assert.NotNull(today);
        Assert.Equal(session.Id, today!.WorkoutSessionId);
        Assert.True(today.IsRunning);
        Assert.True((today.StartedAt - session.StartedAt).Duration() < TimeSpan.FromMilliseconds(1));

        // The workout closes in one request: the marks, the effort, the level and the note.
        var finished = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest(
                "intenso",
                "Senti o ombro.",
                [new PerformedExerciseRequest(exercise.Id, 10, 65m)]));

        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);

        var closed = (await finished.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        Assert.NotNull(closed.FinishedAt);
        Assert.True(closed.DurationSeconds >= 0);
        Assert.Equal("intenso", closed.Intensity);
        Assert.Equal("Senti o ombro.", closed.Notes);

        // The four sets come from the prescription; the repetitions and the load are the athlete's.
        Assert.Equal(4, closed.Sets.Count);
        Assert.All(closed.Sets, set =>
        {
            Assert.Equal(exercise.Id, set.ExerciseId);
            Assert.Equal(65m, set.LoadKilograms);
            Assert.Equal(10, set.Repetitions);
        });

        // The marked exercise is the one that was ticked.
        Assert.Equal([exercise.Id], closed.CompletedExerciseIds);

        // A finished workout cannot be finished twice.
        var lateFinish = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest("moderado", null, [new PerformedExerciseRequest(exercise.Id, 10, 65m)]));

        Assert.Equal(HttpStatusCode.BadRequest, lateFinish.StatusCode);

        var history = await athlete.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        var only = Assert.Single(history!);

        Assert.Equal(closed.DurationSeconds, only.DurationSeconds);
        Assert.Equal(10, only.Sets[0].Repetitions);
        Assert.Equal(65m, only.Sets[0].LoadKilograms);
        Assert.Equal("intenso", only.Intensity);
        Assert.NotNull(only.Volume);
        Assert.Equal(2600m, only.Volume!.PerformedKilograms);
        Assert.Equal(2000m, only.Volume.PrescribedKilograms);
    }

    [RequiresPostgresFact]
    public async Task Athlete_FinishesWithTheRepetitionsTheyReallyDid()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Repetições");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 10, 50m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var closed = await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 2, 50m));

        Assert.All(closed.Sets, set => Assert.Equal(2, set.Repetitions));
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithoutEveryExerciseMarked_CannotCloseTheWorkout()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var first = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Tick Um");
        var second = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Tick Dois");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer,
            athlete.UserId,
            Prescriptions.For(first.Id, 3, 10, 40m),
            Prescriptions.For(second.Id, 3, 10, 40m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        // With one exercise still waiting for its mark, the workout does not close.
        var early = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest(
                "moderado",
                null,
                [new PerformedExerciseRequest(first.Id, 10, 40m)]));

        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal("session.finish.exercises.pending", await early.ReadProblemTitleAsync());

        // With every exercise marked, the workout closes.
        var finished = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest(
                "moderado",
                null,
                [
                    new PerformedExerciseRequest(first.Id, 10, 40m),
                    new PerformedExerciseRequest(second.Id, 10, 40m),
                ]));

        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);

        var closed = (await finished.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        Assert.NotNull(closed.FinishedAt);
        Assert.Equal(
            new[] { first.Id, second.Id }.OrderBy(id => id),
            closed.CompletedExerciseIds.OrderBy(id => id));
    }

    [RequiresPostgresFact]
    public async Task Athlete_ClosingWithAnUnknownLevel_IsRefused()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Nível");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var refused = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest(
                "impossível",
                null,
                [new PerformedExerciseRequest(exercise.Id, 12, 40m)]));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("session.finish.intensity.invalid", await refused.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotStartASecondWorkoutOnTheSameDay()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Um Por Dia");

        var series = await trainer.CreateSeriesAsync($"Um por dia {Guid.NewGuid():N}", athlete.UserId);
        var first = await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));
        var second = await trainer.AddWorkoutAsync(series.Id, "Costas", Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(first.Id));

        Assert.Equal(HttpStatusCode.Created, started.StatusCode);

        var blocked = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(second.Id));

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("session.start.already_started_today", await blocked.ReadProblemTitleAsync());

        // The day already has one workout: the history holds a single session.
        var history = await athlete.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        Assert.Single(history!);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotStartAnotherWorkoutAfterFinishingTheDay()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Dia Cerrado");

        var series = await trainer.CreateSeriesAsync($"Dia fechado {Guid.NewGuid():N}", athlete.UserId);
        var first = await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));
        var second = await trainer.AddWorkoutAsync(series.Id, "Costas", Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(first.Id));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 12, 40m));

        var blocked = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(second.Id));

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("session.start.already_started_today", await blocked.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task Athlete_CancelsAWorkoutStartedByMistake()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Cancelada");
        var workoutId = await SeriesArrange.AssignSeriesAsync(trainer, athlete.UserId, Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var cancelled = await athlete.Client.DeleteAsync($"/sessions/{session.Id}");
        var history = await athlete.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Empty(history!);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotStartAWorkoutFromAnotherSeries()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Alheia");

        await SeriesArrange.AssignSeriesAsync(trainer, athlete.UserId, Prescriptions.For(exercise.Id));

        var foreign = await trainer.CreateSeriesAsync($"Não atribuída {Guid.NewGuid():N}");
        var foreignWorkout = await trainer.AddWorkoutAsync(
            foreign.Id, "Peito", Prescriptions.For(exercise.Id));

        var response = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(foreignWorkout.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithoutAnAssignment_CannotStartAWorkout()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Sem Vínculo");

        var series = await trainer.CreateSeriesAsync($"Sem vínculo {Guid.NewGuid():N}");
        var workout = await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        var response = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workout.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithAnExpiredTerm_ResumesARunningWorkoutButCannotStartAnother()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Vencida");

        var series = await trainer.CreateSeriesAsync($"Vencida {Guid.NewGuid():N}", athlete.UserId);
        var first = await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));
        var second = await trainer.AddWorkoutAsync(series.Id, "Costas", Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(first.Id));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        await factory.ExpireSeriesAsync(athlete.UserId);

        // The workout already in progress still resumes and still closes: only a new start is blocked.
        var resumed = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(first.Id));

        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);

        var closed = await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 12, 40m));

        Assert.NotNull(closed.FinishedAt);

        var blocked = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(second.Id));
        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("workout_series.assignment.expired", await blocked.ReadProblemTitleAsync());
        Assert.True(mine.IsExpired);
        Assert.Equal(0, mine.DaysRemaining);
    }

    [RequiresPostgresFact]
    public async Task Athlete_ReachesOnlyTheirOwnSessions()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var other = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Própria");
        var workoutId = await SeriesArrange.AssignSeriesAsync(trainer, athlete.UserId, Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        // Somebody else's session answers as if it did not exist, and their history stays empty.
        var finished = await other.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/finish",
            new FinishWorkoutRequest(
                "moderado",
                null,
                [new PerformedExerciseRequest(exercise.Id, 12, 40m)]));
        var cancelled = await other.Client.DeleteAsync($"/sessions/{session.Id}");
        var history = await other.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        Assert.Equal(HttpStatusCode.NotFound, finished.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancelled.StatusCode);
        Assert.Empty(history!);
    }

    [RequiresPostgresFact]
    public async Task Trainer_FollowsTheHistoryOfAnAthlete()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Histórico");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 3, 10, 30m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 10, 35m));

        var followed = await trainer.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>(
            $"/athletes/{athlete.UserId}/workouts");

        var only = Assert.Single(followed!);

        Assert.Equal(session.Id, only.Id);
        Assert.NotNull(only.FinishedAt);
        Assert.Equal(10, Assert.Single(only.Sets).Repetitions);
    }

    [RequiresPostgresFact]
    public async Task Anonymous_CannotStartAWorkout()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/sessions",
            new StartWorkoutRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

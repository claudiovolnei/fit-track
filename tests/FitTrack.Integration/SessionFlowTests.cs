using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The athlete's workout end to end: the workout comes from the series the trainer assigned, the timer
/// starts on the server, the athlete logs only the load — the repetitions come from the prescription —
/// and finishing freezes the duration the history then shows.
/// </summary>
public sealed class SessionFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task Athlete_StartsLogsAndFinishesTheWorkoutOfTheSeries()
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

        // The athlete only fills in the load: the ten repetitions come from the prescription.
        var logged = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 65m));

        Assert.Equal(HttpStatusCode.Created, logged.StatusCode);

        var recorded = (await logged.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        var set = Assert.Single(recorded.Sets);

        Assert.Equal((exercise.Id, 65m, 10), (set.ExerciseId, set.LoadKilograms, set.Repetitions));

        var finished = await athlete.Client.PostAsync($"/sessions/{session.Id}/finish", null);

        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);

        var closed = (await finished.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        Assert.NotNull(closed.FinishedAt);
        Assert.True(closed.DurationSeconds >= 0);
        Assert.Single(closed.Sets);

        // A finished workout takes no more sets and cannot be finished twice.
        var lateSet = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 70m));
        var lateFinish = await athlete.Client.PostAsync($"/sessions/{session.Id}/finish", null);

        Assert.Equal(HttpStatusCode.BadRequest, lateSet.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, lateFinish.StatusCode);

        var history = await athlete.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        var only = Assert.Single(history!);

        Assert.Equal(closed.DurationSeconds, only.DurationSeconds);
        Assert.Equal(10, only.Sets[0].Repetitions);
        Assert.Equal(65m, only.Sets[0].LoadKilograms);
    }

    [RequiresPostgresFact]
    public async Task Athlete_LogsTheSameExerciseTwice_KeepingBothSets()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Duas Séries");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 8, 40m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var first = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 40m));
        var second = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 45m));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var logged = (await second.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        Assert.Equal(new[] { 40m, 45m }, logged.Sets.Select(set => set.LoadKilograms));
        Assert.All(logged.Sets, set => Assert.Equal(8, set.Repetitions));
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
    public async Task Athlete_CannotCancelAWorkoutThatAlreadyHasSets()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Sessão Não Cancelada");
        var workoutId = await SeriesArrange.AssignSeriesAsync(trainer, athlete.UserId, Prescriptions.For(exercise.Id));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 60m));

        var cancelled = await athlete.Client.DeleteAsync($"/sessions/{session.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, cancelled.StatusCode);
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

        // The workout already in progress still resumes and still takes sets: only a new start is blocked.
        var resumed = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(first.Id));
        var logged = await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 60m));

        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        Assert.Equal(HttpStatusCode.Created, logged.StatusCode);

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
        var logged = await other.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 60m));
        var finished = await other.Client.PostAsync($"/sessions/{session.Id}/finish", null);
        var cancelled = await other.Client.DeleteAsync($"/sessions/{session.Id}");
        var history = await other.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        Assert.Equal(HttpStatusCode.NotFound, logged.StatusCode);
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

        await athlete.Client.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 35m));
        await athlete.Client.PostAsync($"/sessions/{session.Id}/finish", null);

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

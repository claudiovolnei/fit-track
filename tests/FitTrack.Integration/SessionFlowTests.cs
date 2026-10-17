using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The athlete's workout end to end: the timer starts on the server, the sets are logged while it
/// runs, and finishing freezes the duration that the history then shows.
/// </summary>
public sealed class SessionFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    private const string TrainerEmail = "trainer@fittrack.local";
    private const string TrainerPassword = "Trainer123$";

    [RequiresPostgresFact]
    public async Task Athlete_StartsLogsAndFinishesTheWorkoutOfTheDay()
    {
        var (athlete, optionId) = await ArrangePlanAsync();

        var started = await athlete.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));

        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        Assert.Equal(optionId, session.WorkoutPlanOptionId);
        Assert.Null(session.FinishedAt);
        Assert.Equal(0, session.DurationSeconds);

        // Starting the same option again returns the running workout instead of a second session.
        var again = await athlete.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(
            session.Id,
            (await again.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!.Id);

        // The plan screen shows the timer running, and reloading does not restart it.
        var running = await athlete.ReadMyPlanAsync();
        var today = Assert.Single(running.Options).Today;
        Assert.NotNull(today);
        Assert.Equal(session.Id, today.WorkoutSessionId);
        Assert.True(today.IsRunning);
        Assert.True((today.StartedAt - session.StartedAt).Duration() < TimeSpan.FromMilliseconds(1));

        var exercise = await CreateExerciseAsync();

        var first = await athlete.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 60m, 12));
        var second = await athlete.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 65m, 8));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var finished = await athlete.PostAsync($"/sessions/{session.Id}/finish", null);

        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);
        var closed = (await finished.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        Assert.NotNull(closed.FinishedAt);
        Assert.True(closed.DurationSeconds >= 0);
        Assert.Equal(2, closed.Sets.Count);

        // A finished workout takes no more sets and cannot be finished twice.
        var lateSet = await athlete.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 70m, 6));
        var lateFinish = await athlete.PostAsync($"/sessions/{session.Id}/finish", null);

        Assert.Equal(HttpStatusCode.BadRequest, lateSet.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, lateFinish.StatusCode);

        var history = await athlete.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        var recorded = Assert.Single(history!);
        Assert.Equal(closed.DurationSeconds, recorded.DurationSeconds);
        Assert.Equal(12, recorded.Sets[0].Repetitions);
        Assert.Equal(65m, recorded.Sets[1].LoadKilograms);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotLogASetWithNoRepetitions()
    {
        var (athlete, optionId) = await ArrangePlanAsync();
        var exercise = await CreateExerciseAsync();
        var started = await athlete.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var response = await athlete.PostAsJsonAsync(
            $"/sessions/{session.Id}/sets",
            new LogPerformedSetRequest(exercise.Id, 60m, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotCancelAWorkoutThatAlreadyHasSets()
    {
        var (athlete, optionId) = await ArrangePlanAsync();
        var exercise = await CreateExerciseAsync();
        var started = await athlete.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        await athlete.PostAsJsonAsync($"/sessions/{session.Id}/sets", new LogPerformedSetRequest(exercise.Id, 60m, 12));

        var cancelled = await athlete.DeleteAsync($"/sessions/{session.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, cancelled.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CancelsAWorkoutStartedByMistake()
    {
        var (athlete, optionId) = await ArrangePlanAsync();
        var started = await athlete.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        var cancelled = await athlete.DeleteAsync($"/sessions/{session.Id}");
        var history = await athlete.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");

        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Empty(history!);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotStartAWorkoutFromAnUnknownOption()
    {
        var athlete = await factory.CreateAthleteAsync();

        var response = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotStartAWorkoutFromAnotherAthletesPlan()
    {
        var (_, optionId) = await ArrangePlanAsync();
        var other = await factory.CreateAthleteAsync();

        var response = await other.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(optionId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Anonymous_CannotStartAWorkout()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/sessions", new StartWorkoutRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Assigns a one-option plan to a fresh athlete and gives back the athlete's client.</summary>
    private async Task<(HttpClient Athlete, Guid OptionId)> ArrangePlanAsync()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var athlete = await factory.CreateAthleteAsync();
        var muscleGroup = await trainer.CreateMuscleGroupAsync("Session Group");
        var plan = await trainer.AssignPlanAsync(athlete.UserId);
        var option = await trainer.AddPlanOptionAsync(plan.Id, muscleGroup.Id);

        return (athlete.Client, option.Id);
    }

    /// <summary>Creates a catalog exercise to log sets against, as the trainer.</summary>
    private async Task<ExerciseResponse> CreateExerciseAsync()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var muscleGroup = await trainer.CreateMuscleGroupAsync("Session Exercise Group");

        var created = await trainer.PostAsJsonAsync(
            "/exercises",
            new CreateExerciseRequest($"Session Row {Guid.NewGuid():N}", muscleGroup.Id));

        created.EnsureSuccessStatusCode();

        return (await created.Content.ReadFromJsonAsync<ExerciseResponse>())!;
    }
}

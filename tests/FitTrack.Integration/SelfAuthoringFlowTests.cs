using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The release the administrator gives an athlete to build their own training plan, without a personal
/// trainer: with it the athlete creates and composes a series of their own; without it the same
/// intention is refused.
/// </summary>
public sealed class SelfAuthoringFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    private const string Password = "Athlete123$";

    [RequiresPostgresFact]
    public async Task Athlete_WithoutTheRelease_CannotCreateASeries()
    {
        var athlete = await AthleteWithoutTrainerAsync();

        var listed = await athlete.Client.GetAsync("/workout-series");
        var created = await athlete.Client.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest("Minha série"));

        Assert.Equal(HttpStatusCode.Forbidden, listed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, created.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Admin_ReleasesTheAthlete_WhoThenBuildsTheirOwnSeries()
    {
        var athlete = await AthleteWithoutTrainerAsync();
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);

        var granted = await admin.PostAsync($"/admin/users/{athlete.UserId}/self-authoring", null);

        Assert.Equal(HttpStatusCode.NoContent, granted.StatusCode);

        // The release shows up in the administration list.
        var accounts = await admin.GetFromJsonAsync<IReadOnlyList<UserAccountResponse>>("/admin/users");

        Assert.Contains(accounts!, account => account.UserId == athlete.UserId && account.CanAuthorOwnWorkout);

        // With the release, the athlete creates the series and it is theirs: they are the author and
        // the athlete of it at the same time.
        var created = await athlete.Client.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest("Minha série"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var series = (await created.Content.ReadFromJsonAsync<WorkoutSeriesResponse>())!;

        Assert.Equal(athlete.UserId, series.TrainerId);
        Assert.Equal(athlete.UserId, series.AthleteId);

        // The series is the plan the athlete follows: it comes back on the athlete's own sheet.
        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        Assert.Equal(series.Id, mine.Series.Id);
        Assert.True(mine.IsSelfAuthored);
    }

    [RequiresPostgresFact]
    public async Task ReleasedAthlete_ComposesTheWorkoutsOfTheirOwnSeries()
    {
        var athlete = await AthleteWithoutTrainerAsync();
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);

        await admin.PostAsync($"/admin/users/{athlete.UserId}/self-authoring", null);

        var created = await athlete.Client.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest("Minha série"));

        var series = (await created.Content.ReadFromJsonAsync<WorkoutSeriesResponse>())!;

        // The catalog is readable by any signed-in account, so the athlete can prescribe from it.
        var exercises = await athlete.Client.GetFromJsonAsync<IReadOnlyList<ExerciseResponse>>("/exercises");
        var exercise = exercises![0];

        var workout = await athlete.Client.PostAsJsonAsync(
            $"/workout-series/{series.Id}/workouts",
            new SetSeriesWorkoutRequest("Peito", [Prescriptions.For(exercise.Id)]));

        Assert.Equal(HttpStatusCode.Created, workout.StatusCode);

        var read = await athlete.Client.GetFromJsonAsync<WorkoutSeriesResponse>($"/workout-series/{series.Id}");

        Assert.Equal("Peito", Assert.Single(read!.Workouts).Name);
    }

    [RequiresPostgresFact]
    public async Task Admin_TakesTheReleaseBack_AndTheAthleteIsRefusedAgain()
    {
        var athlete = await AthleteWithoutTrainerAsync();
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);

        await admin.PostAsync($"/admin/users/{athlete.UserId}/self-authoring", null);
        await admin.DeleteAsync($"/admin/users/{athlete.UserId}/self-authoring");

        var created = await athlete.Client.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest("Minha série"));

        Assert.Equal(HttpStatusCode.Forbidden, created.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithoutTheRelease_CannotReachTheSeriesOfSomebodyElse()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await AthleteWithoutTrainerAsync();

        var series = await trainer.CreateSeriesAsync($"Alheia {Guid.NewGuid():N}");

        var read = await athlete.Client.GetAsync($"/workout-series/{series.Id}");
        var renamed = await athlete.Client.PutAsJsonAsync(
            $"/workout-series/{series.Id}",
            new SetWorkoutSeriesNameRequest($"Tomada {Guid.NewGuid():N}"));

        // The athlete has no release: the API refuses before even looking at the series.
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, renamed.StatusCode);
    }

    /// <summary>
    /// A fresh athlete with no personal trainer on the roster: the release is what opens the door to
    /// their own plan.
    /// </summary>
    private async Task<AthleteHandle> AthleteWithoutTrainerAsync()
    {
        var email = $"atleta-livre-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();

        var userId = await client.RegisterAccountAsync(email, Password);
        await client.SignInAsync(email, Password);

        return new AthleteHandle(userId, email, client);
    }
}

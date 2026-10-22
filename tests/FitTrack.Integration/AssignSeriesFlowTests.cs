using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// Assigning a series that already exists, end to end: the author points their roster at it, the
/// followers read is the author's alone, and a session logged from the series names the series it
/// came from in the history.
/// </summary>
public sealed class AssignSeriesFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task Trainer_AssignsAnExistingSeriesToOneAthlete()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Vinculação");

        // The series is born for nobody: the assignment names the destinations after the fact.
        var created = await trainer.CreateSeriesAsync($"Aproveitada {Guid.NewGuid():N}");
        var workout = await trainer.AddWorkoutAsync(created.Id, "Peito", Prescriptions.For(exercise.Id));
        var athlete = await factory.CreateAthleteAsync();

        var assigned = await trainer.PostAsJsonAsync(
            $"/workout-series/{created.Id}/athletes",
            new AssignSeriesRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);

        // The athlete follows the series they did not get born with.
        Assert.Equal(created.Id, (await athlete.Client.ReadMySeriesAsync())!.Series.Id);

        var followers = await trainer.GetFromJsonAsync<IReadOnlyList<SeriesFollowerResponse>>(
            $"/workout-series/{created.Id}/athletes");

        var single = Assert.Single(followers!);

        Assert.Equal(athlete.UserId, single.UserId);
        Assert.True(single.IsOpen);
        Assert.Equal(workout.Id, (await athlete.Client.ReadMySeriesAsync())!.Series.Workouts.Single().Id);
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssignsToSeveralAthletes_AtOnce()
    {
        var trainer = await factory.TrainerAsync();
        var first = await factory.CreateAthleteAsync();
        var second = await factory.CreateAthleteAsync();

        var series = await trainer.CreateSeriesAsync($"Compartilhada {Guid.NewGuid():N}");

        var assigned = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/athletes",
            new AssignSeriesRequest(AthleteIds: [first.UserId, second.UserId, first.UserId]));

        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);

        Assert.Equal(series.Id, (await first.Client.ReadMySeriesAsync())!.Series.Id);
        Assert.Equal(series.Id, (await second.Client.ReadMySeriesAsync())!.Series.Id);

        var followers = await trainer.GetFromJsonAsync<IReadOnlyList<SeriesFollowerResponse>>(
            $"/workout-series/{series.Id}/athletes");

        // The duplication keeps only the first occurrence: two athletes, not three.
        Assert.Equal(2, followers!.Count);
    }

    [RequiresPostgresFact]
    public async Task ForeignTrainer_AssigningOrReadingTheSeries_IsRefusedAsIfItDidNotExistForTheRead()
    {
        var author = await factory.TrainerAsync();
        var foreign = await factory.NewTrainerAsync();
        var athlete = await factory.CreateAthleteAsync();

        var series = await author.CreateSeriesAsync($"De outro {Guid.NewGuid():N}");

        var attempted = await foreign.PostAsJsonAsync(
            $"/workout-series/{series.Id}/athletes",
            new AssignSeriesRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.Forbidden, attempted.StatusCode);

        // The mutation states "not yours"; the read answers as if the series was not there at all.
        var read = await foreign.GetAsync($"/workout-series/{series.Id}/athletes");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Null(await athlete.Client.ReadMySeriesAsync());
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssigningAnAthleteOutsideTheRoster_SavesNothing()
    {
        var trainer = await factory.TrainerAsync();
        var linked = await factory.CreateAthleteAsync();
        var stranger = await factory.CreateClient().RegisterAccountAsync($"athlete-{Guid.NewGuid():N}@fittrack.local", "Athlete123$");

        var series = await trainer.CreateSeriesAsync($"Roster {Guid.NewGuid():N}");

        var attempted = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/athletes",
            new AssignSeriesRequest(AthleteIds: [linked.UserId, stranger]));

        Assert.Equal(HttpStatusCode.Forbidden, attempted.StatusCode);

        var followers = await trainer.GetFromJsonAsync<IReadOnlyList<SeriesFollowerResponse>>(
            $"/workout-series/{series.Id}/athletes");

        Assert.Empty(followers!);
        Assert.Null(await linked.Client.ReadMySeriesAsync());
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssigningWithoutAnyAthlete_IsRefused()
    {
        var trainer = await factory.TrainerAsync();
        var series = await trainer.CreateSeriesAsync($"Sem destino {Guid.NewGuid():N}");

        var attempted = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/athletes",
            new AssignSeriesRequest());

        // Nobody to prescribe to is no prescription at all: refused at the edge.
        Assert.Equal(HttpStatusCode.BadRequest, attempted.StatusCode);

        var followers = await trainer.GetFromJsonAsync<IReadOnlyList<SeriesFollowerResponse>>(
            $"/workout-series/{series.Id}/athletes");

        Assert.Empty(followers!);
    }

    [RequiresPostgresFact]
    public async Task History_NamesTheSeriesTheWorkoutCameFrom()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Histórico");
        var athlete = await factory.CreateAthleteAsync();

        var series = await trainer.CreateSeriesAsync($"Chamada {Guid.NewGuid():N}");
        var workout = await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        // The assignment carries the series to the athlete; the session names it afterwards.
        var assigned = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/athletes",
            new AssignSeriesRequest(AthleteIds: [athlete.UserId]));

        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workout.Id));

        await started.EnsureSuccessWithBodyAsync();

        var history = await athlete.Client.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>("/sessions");
        var named = Assert.Single(history!.Where(session => session.WorkoutSeriesName is not null));

        Assert.Equal(series.Name, named.WorkoutSeriesName);
        Assert.Equal(workout.Id, named.WorkoutSeriesWorkoutId);
    }
}

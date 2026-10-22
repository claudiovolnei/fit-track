using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The cycle of a series over HTTP: the personal trainer ends the cycle of an athlete and the
/// athlete is left without a series, so the next one can be created when the time comes - carrying
/// the sheet of the cycle that just ended.
/// </summary>
public sealed class SeriesCycleFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task PersonalTrainer_EndsTheCycleAndTheAthleteIsLeftWithoutASeries()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Encerrada");
        var series = await trainer.CreateSeriesAsync($"Encerrada {Guid.NewGuid():N}", athlete.UserId);
        await trainer.AddWorkoutAsync(series.Id, "Peito e Tríceps", Prescriptions.For(exercise.Id, 4, 10, 60m));

        var ended = await trainer.PostAsJsonAsync($"/athletes/{athlete.UserId}/series/end", new { });

        Assert.Equal(HttpStatusCode.NoContent, ended.StatusCode);

        // The athlete follows nothing from now on: the sheet is gone from the day screen.
        Assert.Null(await athlete.Client.ReadMySeriesAsync());

        var athletes = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>("/athletes");
        var row = Assert.Single(athletes!, candidate => candidate.UserId == athlete.UserId);

        Assert.Null(row.WorkoutSeriesId);
        Assert.Null(row.WorkoutSeriesName);
        Assert.Equal(series.Name, row.EndedSeriesName);
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_EndsTheCycleAndTheNextSeriesStartsFromTheSameSheet()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Renovada");
        var series = await trainer.CreateSeriesAsync($"Renovada {Guid.NewGuid():N}", athlete.UserId);
        await trainer.AddWorkoutAsync(series.Id, "Peito e Tríceps", Prescriptions.For(exercise.Id, 4, 10, 60m));

        var ended = await trainer.PostAsJsonAsync($"/athletes/{athlete.UserId}/series/end", new { });
        ended.EnsureSuccessStatusCode();

        var created = await trainer.PostAsJsonAsync(
            "/workout-series",
            new CreateWorkoutSeriesRequest($"Renovada fase 2 {Guid.NewGuid():N}", athlete.UserId, CopyPreviousWorkouts: true));

        await created.EnsureSuccessWithBodyAsync();

        var next = (await created.Content.ReadFromJsonAsync<WorkoutSeriesResponse>())!;
        var workout = Assert.Single(next.Workouts);
        var prescribed = Assert.Single(workout.Exercises);

        Assert.Equal("Peito e Tríceps", workout.Name);
        Assert.Equal(exercise.Id, prescribed.ExerciseId);
        Assert.Equal((4, 10, 60m), (prescribed.Sets, prescribed.Repetitions, prescribed.LoadKilograms));

        // The new cycle is a fresh term on the new series.
        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        Assert.Equal(next.Id, mine.Series.Id);
        Assert.False(mine.IsExpired);
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_EndsTheCycleOfAnAthleteOutsideTheirRoster_Fails()
    {
        var trainer = await factory.TrainerAsync();
        var other = await factory.NewTrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var series = await trainer.CreateSeriesAsync($"Alheia {Guid.NewGuid():N}", athlete.UserId);

        var ended = await other.PostAsJsonAsync($"/athletes/{athlete.UserId}/series/end", new { });

        Assert.Equal(HttpStatusCode.Forbidden, ended.StatusCode);
        Assert.Equal(series.Id, (await athlete.Client.ReadMySeriesAsync())!.Series.Id);
    }

    [RequiresPostgresFact]
    public async Task Administrator_EndsTheCycle_Fails()
    {
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var series = await trainer.CreateSeriesAsync($"Do Administrador {Guid.NewGuid():N}", athlete.UserId);

        var ended = await admin.PostAsJsonAsync($"/athletes/{athlete.UserId}/series/end", new { });

        Assert.Equal(HttpStatusCode.Forbidden, ended.StatusCode);
        Assert.Equal(series.Id, (await athlete.Client.ReadMySeriesAsync())!.Series.Id);
    }
}

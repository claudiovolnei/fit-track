using System.Net.Http.Json;
using FitTrack.Adapters.Out.Persistence;
using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Integration.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Integration;

/// <summary>
/// The volume of a session against the current prescription of its workout, exposed by the history
/// envelope. The prescription in these tests is 4 x 10 x 50 kg, so the full prescribed volume is
/// 2,000 kg and a closed workout with 60 kg records 4 x 10 x 60 = 2,400 kg on the performed side.
/// </summary>
public sealed class VolumeFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task History_ComparesThePerformedVolumeWithThePrescription()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Volume Peito");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 10, 50m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

        await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 10, 60m));

        var history = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            "/sessions?page=1&pageSize=10");

        Assert.NotNull(history);
        var item = Assert.Single(history!.Items);

        Assert.NotNull(item.Volume);
        Assert.Equal(2400m, item.Volume!.PerformedKilograms);
        Assert.Equal(2000m, item.Volume.PrescribedKilograms);
        Assert.Equal(400m, item.Volume.DeltaKilograms);
        Assert.Equal(20m, item.Volume.DeltaPercent);

        var detail = Assert.Single(item.Volume.Exercises);
        Assert.Equal(exercise.Id, detail.ExerciseId);
        Assert.Equal(exercise.Name, detail.ExerciseName);
        Assert.Equal("matched", detail.Status);
        Assert.Equal(2400m, detail.PerformedKilograms);
        Assert.Equal(2000m, detail.PrescribedKilograms);

        Assert.NotNull(history.Summary);
        Assert.Equal(1, history.Summary!.SessionCount);
        Assert.Equal(4, history.Summary.SetCount);
        Assert.Equal(2400m, history.Summary.PerformedKilograms);
        Assert.Equal(2000m, history.Summary.PrescribedKilograms);
        Assert.Equal(0, history.Summary.SessionsWithoutPrescription);
    }

    [RequiresPostgresFact]
    public async Task History_SearchesByTheWorkoutNameAndSkipsSessionsWithoutOne()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Volume Busca");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 3, 10, 40m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 10, 40m));

        var found = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            $"/sessions?search={Uri.EscapeDataString("Peito")}&page=1&pageSize=10");

        Assert.NotNull(found);
        Assert.Equal(1, found!.TotalCount);
        Assert.Equal(session.Id, Assert.Single(found.Items).Id);

        var missing = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            $"/sessions?search={Uri.EscapeDataString("Inexistente")}&page=1&pageSize=10");

        Assert.NotNull(missing);
        Assert.Equal(0, missing!.TotalCount);
        Assert.Empty(missing.Items);
    }

    [RequiresPostgresFact]
    public async Task History_MarksAnInProgressSessionAsPartial()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Volume Parcial");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 10, 50m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));

        // The workout is still running: the marks are on the screen, so the server holds nothing yet.
        var history = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            "/sessions?page=1&pageSize=10");

        var item = Assert.Single(history!.Items);

        Assert.Null(item.FinishedAt);
        Assert.NotNull(item.Volume);
        Assert.Equal(0m, item.Volume!.PerformedKilograms);
        Assert.Equal(2000m, item.Volume.PrescribedKilograms);
    }

    [RequiresPostgresFact]
    public async Task History_LeavesTheVolumeNullWhenTheWorkoutWasRemovedFromTheSeries()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Volume Removido");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer, athlete.UserId, Prescriptions.For(exercise.Id, 4, 10, 50m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        await athlete.Client.FinishWorkoutAsync(
            session.Id,
            "moderado",
            null,
            new PerformedExerciseRequest(exercise.Id, 10, 60m));

        var athletes = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>(
            $"/athletes?search={Uri.EscapeDataString(athlete.Email)}");
        var seriesId = Assert.Single(athletes!).WorkoutSeriesId!.Value;

        var removed = await trainer.DeleteAsync($"/workout-series/{seriesId}/workouts/{workoutId}");
        removed.EnsureSuccessStatusCode();

        var history = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            "/sessions?page=1&pageSize=10");

        Assert.NotNull(history);
        var item = Assert.Single(history!.Items);

        Assert.Equal(session.Id, item.Id);
        Assert.Null(item.Volume);
        Assert.NotNull(history.Summary);
        Assert.Equal(1, history.Summary!.SessionsWithoutPrescription);
        Assert.Equal(2400m, history.Summary.PerformedKilograms);
        Assert.Equal(0m, history.Summary.PrescribedKilograms);
    }

    [RequiresPostgresFact]
    public async Task History_LeavesTheVolumeNullForALegacySessionWithoutAWorkout()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Volume Legado");

        var legacy = new WorkoutSessionRecord
        {
            Id = Guid.NewGuid(),
            UserId = athlete.UserId,
            WorkoutDate = new DateOnly(2020, 1, 1),
            WorkoutSeriesWorkoutId = null,
            StartedAt = new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.Zero),
            FinishedAt = new DateTimeOffset(2020, 1, 1, 13, 0, 0, TimeSpan.Zero),
        };

        legacy.Sets.Add(new PerformedSetRecord
        {
            WorkoutSessionId = legacy.Id,
            Position = 0,
            ExerciseId = exercise.Id,
            LoadKilograms = 50m,
            Repetitions = 10,
        });

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FitTrackDbContext>();
            dbContext.WorkoutSessions.Add(legacy);
            await dbContext.SaveChangesAsync();
        }

        var history = await athlete.Client.GetFromJsonAsync<WorkoutHistoryResponse>(
            "/sessions?page=1&pageSize=10");

        Assert.NotNull(history);
        var item = Assert.Single(history!.Items);

        Assert.Null(item.Volume);
        Assert.Equal(legacy.Id, item.Id);
        Assert.NotNull(history.Summary);
        Assert.Equal(1, history.Summary!.SessionsWithoutPrescription);
        Assert.Equal(500m, history.Summary.PerformedKilograms);
    }
}

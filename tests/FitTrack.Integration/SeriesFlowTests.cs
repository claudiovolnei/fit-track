using System.Net;
using System.Net.Http.Json;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The trainer's series end to end: a series is authored once, its workouts keep their order and their
/// labels, the prescription round-trips through PostgreSQL, and the athlete reads back the same sheet
/// after the series is assigned. Since the series is a reusable template, the same one serves more than
/// one athlete — and one trainer never reaches another's series.
/// </summary>
public sealed class SeriesFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task Trainer_BuildsASeriesAndReadsItBackInOrder()
    {
        var trainer = await factory.TrainerAsync();
        var chest = await SeriesArrange.NewExerciseAsync(trainer, "Série Peito");
        var back = await SeriesArrange.NewExerciseAsync(trainer, "Série Costas");

        var created = await trainer.CreateSeriesAsync($"Hipertrofia {Guid.NewGuid():N}");

        Assert.Empty(created.Workouts);

        var first = await trainer.AddWorkoutAsync(
            created.Id, "Peito e Tríceps", Prescriptions.For(chest.Id, 4, 10, 50m));
        var second = await trainer.AddWorkoutAsync(created.Id, "Costas e Bíceps", Prescriptions.For(back.Id));
        var third = await trainer.AddWorkoutAsync(
            created.Id, "Core", Prescriptions.For(chest.Id, 3, 20, loadKilograms: null));

        // The label is read from the position, so the two can never disagree.
        Assert.Equal((1, "A"), (first.Position, first.Label));
        Assert.Equal((2, "B"), (second.Position, second.Label));
        Assert.Equal((3, "C"), (third.Position, third.Label));

        var read = await trainer.GetFromJsonAsync<WorkoutSeriesResponse>($"/workout-series/{created.Id}");

        Assert.NotNull(read);
        Assert.Equal(created.Name, read!.Name);
        Assert.Equal(created.TrainerId, read.TrainerId);
        Assert.Equal(new[] { "A", "B", "C" }, read.Workouts.Select(workout => workout.Label));
        Assert.Equal(
            new[] { "Peito e Tríceps", "Costas e Bíceps", "Core" },
            read.Workouts.Select(workout => workout.Name));

        var prescribed = Assert.Single(read.Workouts[0].Exercises);

        Assert.Equal(chest.Id, prescribed.ExerciseId);
        Assert.Equal(chest.Name, prescribed.ExerciseName);
        Assert.Equal(chest.MuscleGroupName, prescribed.MuscleGroupName);
        Assert.Equal((4, 10, 50m), (prescribed.Sets, prescribed.Repetitions, prescribed.LoadKilograms));

        // A bodyweight exercise prescribes no load, and the null survives the round trip.
        Assert.Null(read.Workouts[2].Exercises.Single().LoadKilograms);
    }

    [RequiresPostgresFact]
    public async Task Trainer_RemovingAWorkout_RenumbersTheLabelsThatFollow()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Renumera");

        var series = await trainer.CreateSeriesAsync($"Renumera {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));
        var middle = await trainer.AddWorkoutAsync(series.Id, "Costas", Prescriptions.For(exercise.Id));
        await trainer.AddWorkoutAsync(series.Id, "Pernas", Prescriptions.For(exercise.Id));

        var removed = await trainer.DeleteAsync($"/workout-series/{series.Id}/workouts/{middle.Id}");

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var read = await trainer.GetFromJsonAsync<WorkoutSeriesResponse>($"/workout-series/{series.Id}");

        Assert.Equal(new[] { 1, 2 }, read!.Workouts.Select(workout => workout.Position));
        Assert.Equal(new[] { "A", "B" }, read.Workouts.Select(workout => workout.Label));
        Assert.Equal(new[] { "Peito", "Pernas" }, read.Workouts.Select(workout => workout.Name));
    }

    [RequiresPostgresFact]
    public async Task Trainer_ReplacingAWorkout_SwapsTheNameAndThePrescriptionKeepingItsPlace()
    {
        var trainer = await factory.TrainerAsync();
        var chest = await SeriesArrange.NewExerciseAsync(trainer, "Série Troca Peito");
        var back = await SeriesArrange.NewExerciseAsync(trainer, "Série Troca Costas");

        var series = await trainer.CreateSeriesAsync($"Troca {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(chest.Id));
        var second = await trainer.AddWorkoutAsync(series.Id, "Costas", Prescriptions.For(back.Id));

        var changed = await trainer.PutAsJsonAsync(
            $"/workout-series/{series.Id}/workouts/{second.Id}",
            new SetSeriesWorkoutRequest("Costas e Bíceps", [Prescriptions.For(chest.Id, 5, 5, 80m)]));

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        var updated = (await changed.Content.ReadFromJsonAsync<WorkoutSeriesWorkoutResponse>())!;

        Assert.Equal(second.Id, updated.Id);
        Assert.Equal("B", updated.Label);
        Assert.Equal("Costas e Bíceps", updated.Name);

        var prescribed = Assert.Single(updated.Exercises);

        Assert.Equal(chest.Id, prescribed.ExerciseId);
        Assert.Equal((5, 5, 80m), (prescribed.Sets, prescribed.Repetitions, prescribed.LoadKilograms));

        // The workout that stayed keeps its own name and prescription.
        var read = await trainer.GetFromJsonAsync<WorkoutSeriesResponse>($"/workout-series/{series.Id}");
        var untouched = Assert.Single(read!.Workouts[0].Exercises);

        Assert.Equal("Peito", read.Workouts[0].Name);
        Assert.Equal((3, 12, 40m), (untouched.Sets, untouched.Repetitions, untouched.LoadKilograms));

        var swapped = Assert.Single(read.Workouts[1].Exercises);

        Assert.Equal("Costas e Bíceps", read.Workouts[1].Name);
        Assert.Equal((5, 5, 80m), (swapped.Sets, swapped.Repetitions, swapped.LoadKilograms));
    }

    [RequiresPostgresFact]
    public async Task Trainer_RenamingASeries_KeepsItsWorkouts()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Renomeia");

        var series = await trainer.CreateSeriesAsync($"Antes {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        var renamed = $"Depois {Guid.NewGuid():N}";

        var response = await trainer.PutAsJsonAsync(
            $"/workout-series/{series.Id}",
            new SetWorkoutSeriesNameRequest(renamed));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = (await response.Content.ReadFromJsonAsync<WorkoutSeriesResponse>())!;

        Assert.Equal(renamed, dto.Name);
        Assert.Single(dto.Workouts);
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssignsASeriesAndTheAthleteReadsTheSameSheet()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Atribuída");

        var series = await trainer.CreateSeriesAsync($"Atribuída {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito e Tríceps", Prescriptions.For(exercise.Id, 4, 10, 60m));

        var assigned = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);

        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        Assert.Equal(series.Id, mine.Series.Id);
        Assert.Equal(series.TrainerId, mine.Series.TrainerId);
        Assert.Equal(SeriesArrange.TrainerName, mine.TrainerName);
        Assert.False(mine.IsExpired);
        Assert.Equal(WorkoutSeriesAssignment.TermInDays, mine.DaysRemaining);
        Assert.True(mine.ExpiresAt > DateTimeOffset.UtcNow);

        var workout = Assert.Single(mine.Series.Workouts);

        Assert.Equal("A", workout.Label);
        Assert.Equal("Peito e Tríceps", workout.Name);
        Assert.Null(workout.Today);

        var prescribed = Assert.Single(workout.Exercises);

        Assert.Equal(exercise.Id, prescribed.ExerciseId);
        Assert.Equal(exercise.Name, prescribed.ExerciseName);
        Assert.Equal((4, 10, 60m), (prescribed.Sets, prescribed.Repetitions, prescribed.LoadKilograms));
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssigningTheSameSeriesAgain_KeepsTheTermRunning()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Reatribuída");

        var series = await trainer.CreateSeriesAsync($"Reatribuída {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        var first = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));
        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        var again = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var reassigned = (await athlete.Client.ReadMySeriesAsync())!;

        // Nothing was restarted: the same term is still counting from the first assignment.
        Assert.Equal(mine.ExpiresAt, reassigned.ExpiresAt);

        var athletes = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>("/athletes");
        var row = Assert.Single(athletes!, candidate => candidate.UserId == athlete.UserId);

        Assert.Equal(series.Id, row.WorkoutSeriesId);
        Assert.Equal(series.Name, row.WorkoutSeriesName);
        Assert.Equal(WorkoutSeriesAssignment.TermInDays, row.DaysRemaining);
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssigningAnotherSeries_ReplacesTheLink()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Substituída");

        var first = await trainer.CreateSeriesAsync($"Primeira {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(first.Id, "Peito", Prescriptions.For(exercise.Id));

        var second = await trainer.CreateSeriesAsync($"Segunda {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(second.Id, "Costas", Prescriptions.For(exercise.Id));

        await trainer.PostAsJsonAsync(
            $"/workout-series/{first.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));
        await trainer.PostAsJsonAsync(
            $"/workout-series/{second.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));

        var mine = (await athlete.Client.ReadMySeriesAsync())!;

        // One series per athlete: the replacement is the one the athlete reads.
        Assert.Equal(second.Id, mine.Series.Id);
        Assert.Equal("Costas", Assert.Single(mine.Series.Workouts).Name);

        var athletes = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>("/athletes");
        var row = Assert.Single(athletes!, candidate => candidate.UserId == athlete.UserId);

        Assert.Equal(second.Id, row.WorkoutSeriesId);
        Assert.Equal(second.Name, row.WorkoutSeriesName);
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithoutAnAssignment_ReadsNoSeries()
    {
        var athlete = await factory.CreateAthleteAsync();

        var mine = await athlete.Client.ReadMySeriesAsync();

        Assert.Null(mine);
    }

    [RequiresPostgresFact]
    public async Task Trainer_ReachesOnlyTheSeriesTheyAuthored()
    {
        var owner = await factory.TrainerAsync();
        var intruder = await factory.NewTrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(owner, "Série Alheia");

        var series = await owner.CreateSeriesAsync($"Alheia {Guid.NewGuid():N}");
        var workout = await owner.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        // Reading somebody else's series answers as if it did not exist, so the read never reveals whose
        // athletes the other trainer is training; a mutation states "not yours" instead.
        var read = await intruder.GetAsync($"/workout-series/{series.Id}");
        var rename = await intruder.PutAsJsonAsync(
            $"/workout-series/{series.Id}",
            new SetWorkoutSeriesNameRequest($"Tomada {Guid.NewGuid():N}"));
        var add = await intruder.PostAsJsonAsync(
            $"/workout-series/{series.Id}/workouts",
            new SetSeriesWorkoutRequest("Costas", [Prescriptions.For(exercise.Id)]));
        var change = await intruder.PutAsJsonAsync(
            $"/workout-series/{series.Id}/workouts/{workout.Id}",
            new SetSeriesWorkoutRequest("Costas", [Prescriptions.For(exercise.Id)]));
        var remove = await intruder.DeleteAsync($"/workout-series/{series.Id}/workouts/{workout.Id}");
        var assign = await intruder.PostAsJsonAsync(
            $"/workout-series/{series.Id}/assignments",
            new AssignWorkoutSeriesRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rename.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, change.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, remove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, assign.StatusCode);

        // The owner's series is untouched by all of it.
        var mine = await owner.GetFromJsonAsync<WorkoutSeriesResponse>($"/workout-series/{series.Id}");

        Assert.Equal(series.Name, mine!.Name);
        Assert.Equal("Peito", Assert.Single(mine.Workouts).Name);
    }

    [RequiresPostgresFact]
    public async Task Trainer_SeesOnlyTheirOwnSeriesInTheList()
    {
        // Both accounts are fresh, so each one authored exactly one series and the list is unambiguous.
        var owner = await factory.NewTrainerAsync();
        var other = await factory.NewTrainerAsync();

        var mine = await owner.CreateSeriesAsync($"Minha {Guid.NewGuid():N}");
        await other.CreateSeriesAsync($"Dela {Guid.NewGuid():N}");

        var listed = await owner.GetFromJsonAsync<IReadOnlyList<WorkoutSeriesResponse>>("/workout-series");

        var only = Assert.Single(listed!);

        Assert.Equal(mine.Id, only.Id);
    }

    [RequiresPostgresFact]
    public async Task Athlete_IsRefusedOnTheTrainerSide()
    {
        var athlete = await factory.CreateAthleteAsync();

        var listed = await athlete.Client.GetAsync("/workout-series");
        var created = await athlete.Client.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest("Minha série"));
        var athletes = await athlete.Client.GetAsync("/athletes");

        Assert.Equal(HttpStatusCode.Forbidden, listed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, created.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, athletes.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Anonymous_IsRefusedOnTheSeriesEndpoints()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/workout-series")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/workout-series/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/athletes")).StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Trainer_AssigningToAnUnknownAthlete_IsRefused()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Série Fantasma");

        var series = await trainer.CreateSeriesAsync($"Fantasma {Guid.NewGuid():N}");
        await trainer.AddWorkoutAsync(series.Id, "Peito", Prescriptions.For(exercise.Id));

        var response = await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/assignments",
            new AssignWorkoutSeriesRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

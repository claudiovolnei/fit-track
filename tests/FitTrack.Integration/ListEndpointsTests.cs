using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// Covers the collection endpoints that share the search/pagination contract beyond the exercise
/// catalog: groups, series, athletes and one athlete's workout history. Each test arranges with names
/// carrying a fresh token, so the assertions see only what the test created.
/// </summary>
public sealed class ListEndpointsTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task MuscleGroups_SearchAndPage()
    {
        var trainer = await factory.TrainerAsync();
        var token = Token();
        var prefix = $"PagGrupo{token}";

        var first = await trainer.CreateMuscleGroupAsync(prefix);
        var second = await trainer.CreateMuscleGroupAsync(prefix);
        await trainer.CreateMuscleGroupAsync("Fora do filtro");

        var firstPage = await trainer.GetFromJsonAsync<PagedResultResponse<MuscleGroupResponse>>(
            $"/muscle-groups?search={token}&page=1&pageSize=1");

        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage!.TotalCount);
        Assert.Single(firstPage.Items);
        Assert.True(firstPage.HasNext);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Contains(firstPage.Items[0].Id, new[] { first.Id, second.Id });

        var secondPage = await trainer.GetFromJsonAsync<PagedResultResponse<MuscleGroupResponse>>(
            $"/muscle-groups?search={token}&page=2&pageSize=1");

        Assert.NotNull(secondPage);
        Assert.Single(secondPage!.Items);
        Assert.False(secondPage.HasNext);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);

        var compat = await trainer.GetFromJsonAsync<IReadOnlyList<MuscleGroupResponse>>(
            $"/muscle-groups?search={token}");

        Assert.Equal(2, compat!.Count);
    }

    [RequiresPostgresFact]
    public async Task WorkoutSeries_SearchAndPage()
    {
        var trainer = await factory.TrainerAsync();
        var token = Token();
        var prefix = $"PagSerie{token}";

        await trainer.CreateSeriesAsync($"{prefix} A");
        await trainer.CreateSeriesAsync($"{prefix} B");
        await trainer.CreateSeriesAsync($"Fora do filtro {Guid.NewGuid():N}");

        var page = await trainer.GetFromJsonAsync<PagedResultResponse<WorkoutSeriesResponse>>(
            $"/workout-series?search={token}&page=1&pageSize=1");

        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.True(page.HasNext);

        var compat = await trainer.GetFromJsonAsync<IReadOnlyList<WorkoutSeriesResponse>>(
            $"/workout-series?search={token}");

        Assert.Equal(2, compat!.Count);
    }

    [RequiresPostgresFact]
    public async Task Athletes_ListsTheRosterAndSearchesTheCandidates()
    {
        var trainer = await factory.TrainerAsync();
        var token = Token();

        var firstId = await factory.CreateClient().RegisterAccountAsync(
            $"athlete-{token}-a@fittrack.local",
            "Athlete123$");
        var secondId = await factory.CreateClient().RegisterAccountAsync(
            $"athlete-{token}-b@fittrack.local",
            "Athlete123$");

        // A fresh athlete is on nobody's roster, so the plain read hides them from the trainer.
        var roster = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&page=1&pageSize=50");

        Assert.NotNull(roster);
        Assert.Equal(0, roster!.TotalCount);

        // The candidates read is the one that finds athletes free to link, search included.
        var candidates = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&candidates=true&page=1&pageSize=1");

        Assert.NotNull(candidates);
        Assert.Equal(2, candidates!.TotalCount);
        Assert.Single(candidates.Items);
        Assert.True(candidates.HasNext);
        Assert.Contains(token, candidates.Items[0].Email);

        await (await trainer.LinkTrainerAsync(firstId)).EnsureSuccessWithBodyAsync();
        await (await trainer.LinkTrainerAsync(secondId)).EnsureSuccessWithBodyAsync();

        var page = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&page=1&pageSize=1");

        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.True(page.HasNext);
        Assert.Contains(token, page.Items[0].Email);

        var compat = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>(
            $"/athletes?search={token}");

        Assert.Equal(2, compat!.Count);

        var candidatesAfter = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&candidates=true&page=1&pageSize=50");

        Assert.NotNull(candidatesAfter);
        Assert.Equal(0, candidatesAfter!.TotalCount);
    }

    [RequiresPostgresFact]
    public async Task AthleteWorkouts_SearchByWorkoutNameAndPage()
    {
        var trainer = await factory.TrainerAsync();
        var athlete = await factory.CreateAthleteAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "PagHistorico");
        var workoutId = await SeriesArrange.AssignSeriesAsync(
            trainer,
            athlete.UserId,
            Prescriptions.For(exercise.Id, 3, 10, 40m));

        var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;
        await athlete.Client.PostAsync($"/sessions/{session.Id}/finish", null);

        var page = await trainer.GetFromJsonAsync<WorkoutHistoryResponse>(
            $"/athletes/{athlete.UserId}/workouts?search={Uri.EscapeDataString("Peito")}&page=1&pageSize=10");

        Assert.NotNull(page);
        Assert.Equal(1, page!.TotalCount);
        var item = Assert.Single(page.Items);
        Assert.Equal(session.Id, item.Id);
        Assert.NotNull(item.Volume);

        Assert.NotNull(page.Summary);
        Assert.Equal(1, page.Summary!.SessionCount);
    }

    private static string Token() => Guid.NewGuid().ToString("N")[..8];
}

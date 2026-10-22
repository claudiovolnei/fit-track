using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// Drives the roster rules over HTTP against PostgreSQL: who may link an athlete to a trainer, that
/// one trainer answers for one athlete, and that what leaves the roster stops being visible. The unit
/// suite drives the same rules over the fakes; this one proves the adapter and the database keep the
/// agreement.
/// </summary>
public sealed class RosterFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task PersonalTrainer_LinksAnAthleteAndSeesThemOnTheRoster()
    {
        var trainer = await factory.TrainerAsync();
        var token = Token();
        var athleteId = await RegisterAsync(token);

        var candidatesBefore = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&candidates=true&page=1&pageSize=50");

        Assert.NotNull(candidatesBefore);
        Assert.Equal(1, candidatesBefore!.TotalCount);

        var linked = await trainer.LinkTrainerAsync(athleteId);

        Assert.Equal(HttpStatusCode.NoContent, linked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await trainer.LinkTrainerAsync(athleteId)).StatusCode);

        var candidatesAfter = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&candidates=true&page=1&pageSize=50");

        Assert.NotNull(candidatesAfter);
        Assert.Equal(0, candidatesAfter!.TotalCount);

        var roster = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&page=1&pageSize=50");

        Assert.NotNull(roster);
        Assert.Equal(1, roster!.TotalCount);
        Assert.Null(roster.Items[0].PersonalTrainerEmail);
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CannotTakeAnAthleteFromAnotherRoster()
    {
        var trainer = await factory.TrainerAsync();
        var intruder = await factory.NewTrainerAsync();
        var token = Token();
        var athleteId = await RegisterAsync(token);

        await (await trainer.LinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();

        var attempt = await intruder.LinkTrainerAsync(athleteId);

        Assert.Equal(HttpStatusCode.Conflict, attempt.StatusCode);
        Assert.Equal("roster.busy", await attempt.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task Admin_NamesTheTrainerAndTheRosterReadShowsIt()
    {
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);
        var token = Token();
        var athleteId = await RegisterAsync(token);

        var trainers = await admin.GetFromJsonAsync<PagedResultResponse<UserAccountResponse>>(
            $"admin/users?role=PersonalTrainer&search={Uri.EscapeDataString(SeriesArrange.TrainerEmail)}&page=1&pageSize=50");

        Assert.NotNull(trainers);
        var trainerId = Assert.Single(trainers!.Items).UserId;

        await (await admin.LinkTrainerAsync(athleteId, trainerId)).EnsureSuccessWithBodyAsync();

        var roster = await admin.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&page=1&pageSize=50");

        Assert.NotNull(roster);
        Assert.Equal(SeriesArrange.TrainerEmail, Assert.Single(roster!.Items).PersonalTrainerEmail);
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CannotReadTheHistoryOfAnAthleteOnAnotherRoster()
    {
        var trainer = await factory.TrainerAsync();
        var other = await factory.NewTrainerAsync();
        var athleteId = await RegisterAsync(Token());

        await (await other.LinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();

        var history = await trainer.GetAsync($"/athletes/{athleteId}/workouts");

        Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
        Assert.Equal("forbidden", await history.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CannotPrescribeToAnAthleteOutsideTheirRoster()
    {
        var trainer = await factory.TrainerAsync();
        var other = await factory.NewTrainerAsync();
        var name = $"Fora do roster {Guid.NewGuid():N}";

        // Nobody's athlete: the gather rule refuses the assignment either way, with one message only,
        // so the API does not reveal whose roster an athlete is on.
        var unlinked = await trainer.PostAsJsonAsync(
            "/workout-series", new SetWorkoutSeriesNameRequest(name, await RegisterAsync(Token())));

        Assert.Equal(HttpStatusCode.Forbidden, unlinked.StatusCode);
        Assert.Equal("forbidden", await unlinked.ReadProblemTitleAsync());

        // Somebody else's athlete: the same refusal, never a roster listing through the side door.
        var foreignToken = Token();
        var foreignAthleteId = await RegisterAsync(foreignToken);
        await (await other.LinkTrainerAsync(foreignAthleteId)).EnsureSuccessWithBodyAsync();

        var foreign = await trainer.PostAsJsonAsync(
            "/workout-series", new SetWorkoutSeriesNameRequest($"{name} 2", foreignAthleteId));

        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal("forbidden", await foreign.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_UnlinksTheirOwnAthlete()
    {
        var trainer = await factory.TrainerAsync();
        var token = Token();
        var athleteId = await RegisterAsync(token);

        await (await trainer.LinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();
        Assert.Equal(1, await CountAsync(trainer, $"/athletes?search={token}"));

        var unlinked = await trainer.UnlinkTrainerAsync(athleteId);

        Assert.Equal(HttpStatusCode.NoContent, unlinked.StatusCode);
        Assert.Equal(0, await CountAsync(trainer, $"/athletes?search={token}"));
        Assert.Equal(1, await CountAsync(trainer, $"/athletes?search={token}&candidates=true"));
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CannotUnlinkAnAthleteOfAnotherRoster()
    {
        var trainer = await factory.TrainerAsync();
        var other = await factory.NewTrainerAsync();
        var athleteId = await RegisterAsync(Token());

        await (await other.LinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();

        var attempt = await trainer.UnlinkTrainerAsync(athleteId);

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
        Assert.Equal("forbidden", await attempt.ReadProblemTitleAsync());
    }

    [RequiresPostgresFact]
    public async Task Admin_UnlinksAnAthleteFromAnyRoster()
    {
        var trainer = await factory.TrainerAsync();
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);
        var token = Token();
        var athleteId = await RegisterAsync(token);

        await (await trainer.LinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();
        await (await admin.UnlinkTrainerAsync(athleteId)).EnsureSuccessWithBodyAsync();

        var roster = await trainer.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"/athletes?search={token}&page=1&pageSize=50");

        Assert.NotNull(roster);
        Assert.Equal(0, roster!.TotalCount);
    }

    /// <summary>A fresh account with no trainer at all: the raw side of the link screen's search.</summary>
    private async Task<Guid> RegisterAsync(string token) =>
        await factory.CreateClient().RegisterAccountAsync(
            $"athlete-{token}@fittrack.local",
            "Athlete123$");

    /// <summary>
    /// How many athletes the read finds. The envelope only comes with <c>page</c>/<c>pageSize</c> — the
    /// plain read answers the array — so the count asks for a page.
    /// </summary>
    private async Task<int> CountAsync(HttpClient client, string path)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var page = await client.GetFromJsonAsync<PagedResultResponse<AthleteResponse>>(
            $"{path}{separator}page=1&pageSize=50");

        return page!.TotalCount;
    }

    private static string Token() => Guid.NewGuid().ToString("N")[..8];
}

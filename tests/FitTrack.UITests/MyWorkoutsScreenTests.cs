using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using FitTrack.UI.Api;
using FitTrack.UI.Pages;

namespace FitTrack.UI.Tests;

/// <summary>
/// The workout history: consecutive identical sets collapse into one header the user opens on
/// demand, and the "about the workout" line is always there, even without a written note.
/// </summary>
public sealed class MyWorkoutsScreenTests : ScreenTest
{
    private static readonly Guid FirstSessionId = Guid.NewGuid();

    private static readonly Guid SecondSessionId = Guid.NewGuid();

    private static readonly Guid AthleteUserId = Guid.NewGuid();

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private void ArrangeHistory(params WorkoutSessionResponse[] sessions)
    {
        Api.Answer(HttpMethod.Get, "/exercises", Sheet.Catalog);
        Api.Answer(HttpMethod.Get, "/sessions", Sheet.History(sessions, 1, sessions.Length));
    }

    private void AnswerPerQuery(Func<string, WorkoutHistoryResponse> answer)
    {
        // The request carries the search and the page in the query: the fake answers per call,
        // so the redraw of a search really goes through the load that replaces the page.
        Api.Answer(HttpMethod.Get, "/sessions", request => Json(answer(request.RequestUri!.Query)));
    }

    private static HttpResponseMessage Json(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload, Web), Encoding.UTF8, "application/json"),
    };

    private static bool IsExpanded(IElement header) => header.GetAttribute("aria-expanded") == "true";

    [Fact]
    public void IdenticalConsecutiveSets_CollapseIntoOneHeader()
    {
        ArrangeHistory(Sheet.ChestWorkout(FirstSessionId));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() => Assert.Contains("4 × 20 · 20 kg", cut.Markup));

        var header = cut.Find("button.set-group-toggle");
        Assert.Equal("false", header.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("div.session-set-row"));
    }

    [Fact]
    public void ClickingTheHeader_RevealsTheIndividualRowsWithTheirIndexes()
    {
        ArrangeHistory(Sheet.ChestWorkout(FirstSessionId));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() => Assert.Contains("4 × 20 · 20 kg", cut.Markup));

        cut.Find("button.set-group-toggle").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("button.set-group-toggle").GetAttribute("aria-expanded"));
            Assert.Equal(4, cut.FindAll("div.session-set-row").Count);
        });

        // The chronology of the session, one ordinal per set in the order it was performed.
        var rows = cut.FindAll("div.session-set-row");
        Assert.Contains("1", rows[0].TextContent);
        Assert.Contains("Supino reto", rows[0].TextContent);
        Assert.Contains("4", rows[^1].TextContent);
    }

    [Fact]
    public void AChangedLoad_BreaksTheRuns_InTheOrderOfTheSession()
    {
        var sets = new[]
        {
            new PerformedSetResponse(Sheet.BenchId, 20m, 20),
            new PerformedSetResponse(Sheet.BenchId, 20m, 20),
            new PerformedSetResponse(Sheet.BenchId, 30m, 20),
            new PerformedSetResponse(Sheet.BenchId, 20m, 20),
            new PerformedSetResponse(Sheet.BenchId, 20m, 20),
        };

        var session = new WorkoutSessionResponse(
            FirstSessionId,
            new DateOnly(2026, 1, 5),
            DateTimeOffset.UtcNow.AddMinutes(-90),
            DateTimeOffset.UtcNow.AddMinutes(-48),
            2520,
            null,
            sets);

        ArrangeHistory(session);

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.set-group-toggle")));

        // Three runs: two headers of repeats and the load change in between, straight as a row.
        Assert.Equal(2, cut.FindAll("button.set-group-toggle").Count);
        Assert.Contains("2 × 20 · 20 kg", cut.Markup);
        Assert.Contains("30 kg × 20", cut.Markup);
    }

    [Fact]
    public void ASessionOfASingleSet_RendersTheRowWithoutAnyToggle()
    {
        ArrangeHistory(Sheet.ChestWorkout(FirstSessionId, sets: 1, repetitions: 12, loadKilograms: 40m));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() => Assert.Contains("40 kg × 12", cut.Markup));

        Assert.Empty(cut.FindAll("button.set-group-toggle"));
        Assert.Equal(1, cut.FindAll("div.session-set-row").Count);
        Assert.DoesNotContain("aria-expanded", cut.Markup);
    }

    [Fact]
    public void AWorkoutWithoutAWrittenNote_SaysItWasNotInformed()
    {
        ArrangeHistory(Sheet.ChestWorkout(FirstSessionId));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sobre o treino:", cut.Markup);
            Assert.Contains("não informado", cut.Markup);
        });
    }

    [Fact]
    public void AWorkoutWithAWrittenNote_ShowsWhatTheAthleteWrote()
    {
        ArrangeHistory(Sheet.ChestWorkout(FirstSessionId, notes: "pernas cansadas"));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sobre o treino:", cut.Markup);
            Assert.Contains("pernas cansadas", cut.Markup);
        });
        Assert.DoesNotContain("não informado", cut.Markup);
    }

    [Fact]
    public void TheExpansionSurvivesTheRedrawOfASearch()
    {
        var session = Sheet.ChestWorkout(FirstSessionId);
        var other = Sheet.ChestWorkout(SecondSessionId, sets: 2, repetitions: 12, loadKilograms: 30m, notes: "exercício diferente");

        // Any search still lands on the same two sessions: the fake keys on the query, not on content.
        AnswerPerQuery(_ => Sheet.History([session, other], 1, 2));

        var cut = Render<MyWorkouts>();

        cut.WaitForAssertion(() => Assert.Contains("4 × 20 · 20 kg", cut.Markup));

        cut.FindAll("button.set-group-toggle")[0].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("button.set-group-toggle").Count);
            Assert.True(IsExpanded(cut.Find("button.set-group-toggle")));
        });

        var before = Api.Requests.Count;

        cut.Find("input[type=search]").Input("supino");
        cut.Find("input[type=search]").KeyDown("Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.True(Api.Requests.Count > before, "the search rang through the load.");
            Assert.Equal(2, cut.FindAll("button.set-group-toggle").Count);
            Assert.True(IsExpanded(cut.Find("button.set-group-toggle")),
                "the group the athlete opened of the first session survives the redraw.");
            Assert.False(IsExpanded(cut.FindAll("button.set-group-toggle")[1]),
                "the other sessions stay collapsed.");
        });
    }

    [Fact]
    public void ThePersonalTrainerHistory_CollapsesAndOpensTheSameWay()
    {
        Api.Answer(HttpMethod.Get, "/exercises", Sheet.Catalog);
        Api.Answer(HttpMethod.Get, "/athletes", Sheet.Roster(AthleteUserId));
        Api.Answer(HttpMethod.Get, $"/athletes/{AthleteUserId}/workouts",
            Sheet.History([Sheet.ChestWorkout(FirstSessionId, notes: null)], 1, 1));

        var cut = Render<AthleteWorkouts>(parameters => parameters.Add(page => page.UserId, AthleteUserId));

        cut.WaitForAssertion(() => Assert.Contains("4 × 20 · 20 kg", cut.Markup));

        var header = cut.Find("button.set-group-toggle");
        Assert.Equal("false", header.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("div.session-set-row"));
        Assert.Contains("Sobre o treino:", cut.Markup);
        Assert.Contains("não informado", cut.Markup);

        header.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("button.set-group-toggle").GetAttribute("aria-expanded"));
            Assert.Equal(4, cut.FindAll("div.session-set-row").Count);
        });
    }
}

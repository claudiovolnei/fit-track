using System.Net;
using Bunit;
using FitTrack.UI.Api;
using FitTrack.UI.Components;
using FitTrack.UI.Pages;

namespace FitTrack.UI.Tests;

/// <summary>
/// The athlete's own plan: the screen that creates the series they author, and the state that says
/// the administrator has not released them yet.
/// </summary>
public sealed class MeuPlanoScreenTests : ScreenTest
{
    [Fact]
    public void WithoutTheRelease_TheScreenSaysWhatIsMissing()
    {
        Api.Answer(HttpMethod.Get, "/workout-series/mine", Sheet.Running());
        Api.AnswerProblem(HttpMethod.Get, "/workout-series", HttpStatusCode.Forbidden, "forbidden");

        var cut = Render<MeuPlano>();

        cut.WaitForAssertion(() => Assert.Contains("Montar o próprio treino ainda não está liberado", cut.Markup));

        // The series the athlete could author are not offered: the release is what opens the door.
        Assert.DoesNotContain("Criar série", cut.Markup);
    }

    [Fact]
    public void WithTheRelease_TheScreenOffersTheSeriesOfTheAthlete()
    {
        Api.Answer(HttpMethod.Get, "/workout-series/mine", Sheet.Running(selfAuthored: true));
        Api.Answer(HttpMethod.Get, "/workout-series", new List<WorkoutSeriesResponse>
        {
            new(Sheet.SeriesId, "Meu plano", Sheet.BenchId, Sheet.BenchId, []),
        });

        var cut = Render<MeuPlano>();

        cut.WaitForAssertion(() => Assert.Contains("Meu plano", cut.Markup));

        Assert.Contains("Criar série", cut.Markup);
    }

    [Fact]
    public void CreatingASeries_SendsTheNameOfTheNewPlan()
    {
        Api.Answer(HttpMethod.Get, "/workout-series/mine", Sheet.Running(selfAuthored: true));
        Api.Answer(HttpMethod.Get, "/workout-series", new List<WorkoutSeriesResponse>());
        Api.Answer(HttpMethod.Post, "/workout-series", new WorkoutSeriesResponse(
            Sheet.SeriesId, "Meu plano", Sheet.BenchId, Sheet.BenchId, []));

        var cut = Render<MeuPlano>();

        cut.WaitForAssertion(() => Assert.Contains("Criar série", cut.Markup));

        cut.Find("input#new-plan-name").Input("Hipertrofia A/B/C");

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Criar série", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Single(
            Api.RequestsTo("/workout-series"),
            request => request.Method == "POST"));

        var request = Api.RequestsTo("/workout-series").First(candidate => candidate.Method == "POST");

        Assert.Contains("\"name\":\"Hipertrofia A/B/C\"", request.Body);
    }
}

/// <summary>
/// The card of a finished workout: the level the athlete gave the session and the note they wrote
/// about it.
/// </summary>
public sealed class SessionCardTests : ScreenTest
{
    [Fact]
    public void AFinishedWorkout_ShowsTheLevelAndTheNote()
    {
        var session = new WorkoutSessionResponse(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 5),
            DateTimeOffset.UtcNow.AddMinutes(-42),
            DateTimeOffset.UtcNow,
            2520,
            null,
            [new PerformedSetResponse(Sheet.BenchId, 40m, 10)])
        {
            Intensity = "intenso",
            Notes = "Senti o ombro.",
        };

        var cut = Render<SessionCard>(parameters => parameters
            .Add(card => card.Session, session)
            .Add(card => card.ExerciseNames, new Dictionary<Guid, string> { [Sheet.BenchId] = "Supino reto" }));

        Assert.Contains("Intenso", cut.Markup);
        Assert.Contains("Sobre o treino:", cut.Markup);
        Assert.Contains("Senti o ombro.", cut.Markup);
    }

    [Fact]
    public void AWorkoutWithoutTheLevel_ShowsNeitherTheLevelNorTheNote()
    {
        var session = new WorkoutSessionResponse(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 5),
            DateTimeOffset.UtcNow.AddMinutes(-42),
            DateTimeOffset.UtcNow,
            2520,
            null,
            []);

        var cut = Render<SessionCard>(parameters => parameters
            .Add(card => card.Session, session)
            .Add(card => card.ExerciseNames, new Dictionary<Guid, string>()));

        Assert.DoesNotContain("Sobre o treino:", cut.Markup);
    }
}

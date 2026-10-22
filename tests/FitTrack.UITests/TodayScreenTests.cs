using System.Net;
using System.Text;
using AngleSharp.Dom;
using Bunit;
using FitTrack.UI.Api;
using FitTrack.UI.Pages;
using Microsoft.AspNetCore.Components;

namespace FitTrack.UI.Tests;

/// <summary>
/// The athlete's screen of the day: the marks live on the screen, the effort stays with what the
/// personal trainer prescribed until the athlete says otherwise, and the whole workout travels in
/// one request when the athlete finishes it.
/// </summary>
public sealed class TodayScreenTests : ScreenTest
{
    private void ArrangeRunningWorkout()
    {
        Api.Answer(HttpMethod.Get, "/exercises", Sheet.Catalog);
        Api.Answer(HttpMethod.Get, "/workout-series/mine", Sheet.Running());
    }

    [Fact]
    public void TheMarkOfAnExercise_CostsNoRequest()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        var before = Api.Requests.Count;

        cut.FindAll("input[type=checkbox]")[0].Change(true);

        // The mark is on the screen: the server learns it only when the workout is finished.
        Assert.Equal(before, Api.Requests.Count);
        Assert.Empty(Api.RequestsTo($"/sessions/{Sheet.SessionId}/completion"));
        Assert.Empty(Api.RequestsTo($"/sessions/{Sheet.SessionId}/sets"));
    }

    [Fact]
    public void ThePrescribedValue_StaysOnTheScreen_UntilTheAthleteAdjustsIt()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        Assert.Contains("4 × 10 @ 40 kg", cut.Markup);
    }

    [Fact]
    public void ClickingTheValue_OpensTheFieldsOfTheEffort()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        // Nothing is open: the fields only show when the athlete asks for them.
        Assert.Empty(cut.FindAll("input[id^=set-reps-]"));

        cut.FindAll("button.effort-link")[0].Click();

        var repetitions = cut.Find("input[id^=set-reps-]");
        var load = cut.Find("input[id^=set-load-]");

        // The fields open with what the personal trainer prescribed.
        Assert.Equal("10", repetitions.GetAttribute("value"));
        Assert.Equal("40", load.GetAttribute("value"));
    }

    [Fact]
    public void TheFinishButton_StaysDisabled_WhileAnExerciseIsPending()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        Assert.True(FinishButton(cut).HasAttribute("disabled"));

        cut.FindAll("input[type=checkbox]")[0].Change(true);

        Assert.Contains("Faltam 1 exercício", cut.Markup);
        Assert.True(FinishButton(cut).HasAttribute("disabled"));

        cut.FindAll("input[type=checkbox]")[1].Change(true);

        Assert.DoesNotContain("Faltam", cut.Markup);
        Assert.False(FinishButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void FinishingTheWorkout_SendsOneRequest_WithTheMarksTheLevelAndTheNote()
    {
        ArrangeRunningWorkout();

        Api.Answer(
            HttpMethod.Post,
            $"/sessions/{Sheet.SessionId}/finish",
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            });

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=checkbox]")[1].Change(true);

        FinishButton(cut).Click();

        // The modal asks the level and the note before anything leaves the browser.
        Assert.NotNull(cut.Find("select#finish-intensity"));
        Assert.NotNull(cut.Find("textarea#finish-notes"));

        cut.Find("select#finish-intensity").Change("intenso");
        cut.Find("textarea#finish-notes").Input("Senti o ombro.");

        ModalConfirm(cut).Click();

        cut.WaitForAssertion(() => Assert.Single(Api.RequestsTo($"/sessions/{Sheet.SessionId}/finish")));

        var request = Api.RequestsTo($"/sessions/{Sheet.SessionId}/finish")[0];

        Assert.Contains("\"intensity\":\"intenso\"", request.Body);
        Assert.Contains("\"notes\":\"Senti o ombro.\"", request.Body);
        Assert.Contains($"\"exerciseId\":\"{Sheet.BenchId}\"", request.Body);
        Assert.Contains($"\"exerciseId\":\"{Sheet.RowId}\"", request.Body);
        Assert.Contains("\"repetitions\":10", request.Body);
        Assert.Contains("\"loadKilograms\":40", request.Body);

        // One request carries the whole workout: nothing went out before it.
        Assert.Single(Api.Requests, request => request.Method == "POST");
    }

    [Fact]
    public void TheModal_WaitsForTheLevel_BeforeItClosesTheWorkout()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=checkbox]")[1].Change(true);

        FinishButton(cut).Click();

        Assert.True(ModalConfirm(cut).HasAttribute("disabled"));

        cut.Find("select#finish-intensity").Change("moderado");

        Assert.False(ModalConfirm(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void TheLevelOffersTheFiveLevels()
    {
        ArrangeRunningWorkout();

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=checkbox]")[1].Change(true);

        FinishButton(cut).Click();

        var options = cut.FindAll("select#finish-intensity option")
            .Select(option => option.GetAttribute("value") ?? string.Empty)
            .ToArray();

        Assert.Equal(["", "leve", "medio", "moderado", "intenso", "muitoIntenso"], options);
    }

    [Fact]
    public void TheMarksOfTheAthlete_SurviveARefusedFinish()
    {
        ArrangeRunningWorkout();

        // The server refuses and the screen is read again: the marks are the athlete's, not the
        // server's, so they have to survive the refusal.
        Api.AnswerProblem(
            HttpMethod.Post,
            $"/sessions/{Sheet.SessionId}/finish",
            HttpStatusCode.Conflict,
            "session.finish.exercises.pending");

        var cut = Render<Today>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type=checkbox]")));

        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=checkbox]")[1].Change(true);

        FinishButton(cut).Click();
        cut.Find("select#finish-intensity").Change("moderado");
        ModalConfirm(cut).Click();

        cut.WaitForAssertion(() => Assert.Single(Api.RequestsTo($"/sessions/{Sheet.SessionId}/finish")));

        // The screen was read again after the refusal: both marks are still marked.
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("input[type=checkbox][checked]").Count));
    }

    /// <summary>The button of the page that opens the closing modal.</summary>
    private static IElement FinishButton<TComponent>(IRenderedComponent<TComponent> cut)
        where TComponent : IComponent =>
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Finalizar treino", StringComparison.Ordinal));

    /// <summary>The button of the modal that closes the workout.</summary>
    private static IElement ModalConfirm<TComponent>(IRenderedComponent<TComponent> cut)
        where TComponent : IComponent =>
        cut.FindAll(".modal-card button")
            .First(button => button.TextContent.Contains("Finalizar", StringComparison.Ordinal));
}

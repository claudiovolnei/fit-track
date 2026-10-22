using System.Net;
using Bunit;
using FitTrack.UI.Api;
using FitTrack.UI.Pages;

namespace FitTrack.UI.Tests;

/// <summary>
/// The administration of the accounts: the release that lets an athlete build their own plan, and
/// the chip that says it is on.
/// </summary>
public sealed class AdminUsersScreenTests : ScreenTest
{
    private static readonly Guid AthleteId = Guid.NewGuid();

    private void ArrangeAccounts(bool released)
    {
        Api.Answer(HttpMethod.Get, "/admin/users", new PagedResultResponse<UserAccountResponse>(
            [
                new UserAccountResponse(
                    AthleteId,
                    "Atleta FitTrack",
                    "athlete@fittrack.local",
                    ["User"])
                {
                    CanAuthorOwnWorkout = released,
                },
            ],
            1,
            12,
            1,
            1,
            false,
            false));
    }

    [Fact]
    public void TheAthleteWithoutTheRelease_CanBeGivenIt()
    {
        ArrangeAccounts(released: false);

        Api.Answer(HttpMethod.Post, $"/admin/users/{AthleteId}/self-authoring", new { });

        var cut = Render<AdminUsers>();

        cut.WaitForAssertion(() => Assert.Contains("Permitir montar o próprio treino", cut.Markup));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Permitir montar o próprio treino", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Single(
            Api.RequestsTo($"/admin/users/{AthleteId}/self-authoring"),
            request => request.Method == "POST"));
    }

    [Fact]
    public void TheAthleteWithTheRelease_CanHaveItTakenBack()
    {
        ArrangeAccounts(released: true);

        Api.Answer(HttpMethod.Delete, $"/admin/users/{AthleteId}/self-authoring", new { });

        var cut = Render<AdminUsers>();

        cut.WaitForAssertion(() => Assert.Contains("Montagem própria liberada", cut.Markup));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Revogar montagem própria", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Single(
            Api.RequestsTo($"/admin/users/{AthleteId}/self-authoring"),
            request => request.Method == "DELETE"));
    }

    [Fact]
    public void TheReleaseIsShownOnlyForTheAccountsOfAnAthlete()
    {
        Api.Answer(HttpMethod.Get, "/admin/users", new PagedResultResponse<UserAccountResponse>(
            [
                new UserAccountResponse(
                    Guid.NewGuid(),
                    "Personal de Teste",
                    "trainer@fittrack.local",
                    ["PersonalTrainer"]),
            ],
            1,
            12,
            1,
            1,
            false,
            false));

        var cut = Render<AdminUsers>();

        cut.WaitForAssertion(() => Assert.Contains("Personal de Teste", cut.Markup));

        Assert.DoesNotContain("Permitir montar o próprio treino", cut.Markup);
    }
}

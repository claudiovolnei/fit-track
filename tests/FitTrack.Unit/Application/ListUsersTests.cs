using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class ListUsersTests
{
    private readonly InMemoryUserQueries _query = new();

    [Fact]
    public async Task ExecuteAsync_ReturnsEveryAccountWithItsRoles()
    {
        _query.Seed(Guid.NewGuid(), "Claudio Volnei", "trainer@fittrack.local", Roles.PersonalTrainer);
        _query.Seed(Guid.NewGuid(), "Atleta FitTrack", "athlete@fittrack.local", Roles.User);

        var useCase = new ListUsers(StubCurrentUser.Admin(UserId.New()), _query);

        var accounts = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, accounts.Count);
        Assert.Equal("athlete@fittrack.local", accounts[0].Email);
        Assert.Equal(new[] { Roles.User }, accounts[0].Roles);
        Assert.Equal(new[] { Roles.PersonalTrainer }, accounts[1].Roles);
    }

    [Fact]
    public async Task ExecuteAsync_TellsWhichAccountsWereReleasedToBuildTheirOwnPlan()
    {
        _query.Seed(Guid.NewGuid(), "Atleta FitTrack", "athlete@fittrack.local", Roles.User);
        _query.SeedReleased(Guid.NewGuid(), "Atleta liberated", "liberado@fittrack.local", Roles.User);

        var useCase = new ListUsers(StubCurrentUser.Admin(UserId.New()), _query);

        var accounts = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.False(accounts[0].CanAuthorOwnWorkout);
        Assert.True(accounts[1].CanAuthorOwnWorkout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenTheCallerIsNotAnAdministrator_IsForbidden(bool isPersonalTrainer)
    {
        var useCase = new ListUsers(
            new StubCurrentUser(UserId.New(), isPersonalTrainer, isAdmin: false),
            _query);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => useCase.ExecuteAsync(CancellationToken.None));

        Assert.False(_query.WasQueried);
    }
}

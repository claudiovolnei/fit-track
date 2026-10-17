using FitTrack.Application.Dtos;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class ListUserSessionsTests
{
    private readonly InMemoryUserSessionQueries _query = new();

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyTheCurrentUsersSessions()
    {
        var user = UserId.New();
        var otherUser = UserId.New();
        _query.Seed(user, Session(new DateOnly(2026, 1, 5)));
        _query.Seed(otherUser, Session(new DateOnly(2026, 1, 6)));

        var useCase = new ListUserSessions(StubCurrentUser.Athlete(user), _query);

        var sessions = await useCase.ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(sessions);
        Assert.Equal(new DateOnly(2026, 1, 5), only.WorkoutDate);
    }

    private static WorkoutSessionDto Session(DateOnly date) =>
        new(
            Guid.NewGuid(),
            date,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
            null,
            0,
            null,
            []);
}

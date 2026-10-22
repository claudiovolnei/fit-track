using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public class AutoFinishExpiredWorkoutsTests
{
    [Fact]
    public async Task AutoFinish_ClosesSessionsOlderThanTwoAndHalfHours()
    {
        var now = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        var repo = new InMemoryWorkoutSessionRepository();
        var clock = new FixedClock(now);
        var userId = new UserId(Guid.NewGuid());
        var startedAt = now.AddHours(-3);
        var session = WorkoutSession.StartFromPlannedWorkout(
            new WorkoutSessionId(Guid.NewGuid()),
            userId,
            new WorkoutDate(DateOnly.FromDateTime(startedAt.UtcDateTime)),
            startedAt,
            new WorkoutSeriesWorkoutId(Guid.NewGuid()));
        await repo.SaveAsync(session, CancellationToken.None);

        var useCase = new AutoFinishExpiredWorkouts(repo, clock);
        var finished = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, finished);
        var stored = await repo.GetByIdAsync(session.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.False(stored!.IsRunning);
        Assert.Equal(startedAt.AddHours(2.5), stored.FinishedAt);
    }

    [Fact]
    public async Task AutoFinish_LeavesRecentSessionsRunning()
    {
        var now = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        var repo = new InMemoryWorkoutSessionRepository();
        var clock = new FixedClock(now);
        var userId = new UserId(Guid.NewGuid());
        var startedAt = now.AddHours(-1);
        var session = WorkoutSession.StartFromPlannedWorkout(
            new WorkoutSessionId(Guid.NewGuid()),
            userId,
            new WorkoutDate(DateOnly.FromDateTime(startedAt.UtcDateTime)),
            startedAt,
            new WorkoutSeriesWorkoutId(Guid.NewGuid()));
        await repo.SaveAsync(session, CancellationToken.None);

        var useCase = new AutoFinishExpiredWorkouts(repo, clock);
        var finished = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, finished);
        var stored = await repo.GetByIdAsync(session.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.True(stored!.IsRunning);
    }
}
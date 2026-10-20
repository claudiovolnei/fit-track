using FitTrack.UI.Auth;

namespace FitTrack.Unit.Application;

public sealed class UserAvatarStateTests
{
    [Fact]
    public async Task EnsureLoadedAsync_LoadsOncePerUser()
    {
        var state = new UserAvatarState();
        var calls = 0;

        Task<string?> Load(CancellationToken _)
        {
            calls++;
            return Task.FromResult<string?>("data:image/png;base64,abc");
        }

        await state.EnsureLoadedAsync("user-1", Load);
        await state.EnsureLoadedAsync("user-1", Load);

        Assert.Equal(1, calls);
        Assert.True(state.Loaded);
        Assert.Equal("user-1", state.UserKey);
        Assert.Equal("data:image/png;base64,abc", state.AvatarUrl);
    }

    [Fact]
    public async Task EnsureLoadedAsync_DeduplicatesConcurrentCallers()
    {
        var state = new UserAvatarState();
        var completion = new TaskCompletionSource<string?>();
        var calls = 0;

        Task<string?> Load(CancellationToken _)
        {
            calls++;
            return completion.Task;
        }

        var first = state.EnsureLoadedAsync("user-1", Load);
        var second = state.EnsureLoadedAsync("user-1", Load);

        completion.SetResult("avatar");

        await Task.WhenAll(first, second);

        Assert.Equal(1, calls);
        Assert.Equal("avatar", state.AvatarUrl);
    }

    [Fact]
    public async Task EnsureLoadedAsync_ANewerUserSupersedesThePreviousFetch()
    {
        var state = new UserAvatarState();
        var firstCompletion = new TaskCompletionSource<string?>();
        var secondCompletion = new TaskCompletionSource<string?>();

        var first = state.EnsureLoadedAsync("user-1", _ => firstCompletion.Task);
        var second = state.EnsureLoadedAsync("user-2", _ => secondCompletion.Task);

        secondCompletion.SetResult("avatar-2");
        await second;

        firstCompletion.SetResult("avatar-1");
        await first;

        Assert.Equal("user-2", state.UserKey);
        Assert.Equal("avatar-2", state.AvatarUrl);
    }

    [Fact]
    public async Task Clear_ForgetsTheUserAndIgnoresAnInFlightFetch()
    {
        var state = new UserAvatarState();
        var completion = new TaskCompletionSource<string?>();

        var loading = state.EnsureLoadedAsync("user-1", _ => completion.Task);
        state.Clear();
        completion.SetResult("late-avatar");
        await loading;

        Assert.Null(state.UserKey);
        Assert.Null(state.AvatarUrl);
        Assert.False(state.Loaded);
    }

    [Fact]
    public async Task Set_WinsOverAnInFlightFetch()
    {
        var state = new UserAvatarState();
        var completion = new TaskCompletionSource<string?>();

        var loading = state.EnsureLoadedAsync("user-1", _ => completion.Task);
        state.Set("chosen-avatar");
        completion.SetResult("late-avatar");
        await loading;

        Assert.Equal("chosen-avatar", state.AvatarUrl);
    }

    [Fact]
    public async Task EnsureLoadedAsync_FailureFallsBackToTheInitialsAndStopsRetrying()
    {
        var state = new UserAvatarState();
        var calls = 0;

        Task<string?> Load(CancellationToken _)
        {
            calls++;
            throw new InvalidOperationException("network down");
        }

        await state.EnsureLoadedAsync("user-1", Load);
        await state.EnsureLoadedAsync("user-1", Load);

        Assert.Equal(1, calls);
        Assert.True(state.Loaded);
        Assert.Null(state.AvatarUrl);
    }

    [Fact]
    public async Task EnsureLoadedAsync_IgnoresAnonymousCallers()
    {
        var state = new UserAvatarState();
        var called = false;

        await state.EnsureLoadedAsync("  ", _ =>
        {
            called = true;
            return Task.FromResult<string?>("avatar");
        });

        Assert.False(called);
        Assert.False(state.Loaded);
    }
}

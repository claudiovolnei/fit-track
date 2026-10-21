using FitTrack.UI.Components;

namespace FitTrack.Unit.Application;

/// <summary>
/// The countdown of the rest between sets, as the athlete sees it: the seconds left, the pause that
/// freezes the count, the resume that continues from where it stopped, and the end of the count.
/// The clock is injected, so the rules are proved without waiting for the wall clock.
/// </summary>
public sealed class RestCountdownTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ANewCountdown_HoldsThePrescribedDuration()
    {
        var countdown = new RestCountdown(90, () => Start);

        Assert.Equal(90, countdown.Seconds);
        Assert.Equal(TimeSpan.FromSeconds(90), countdown.Remaining);
        Assert.False(countdown.Paused);
        Assert.False(countdown.Finished);
    }

    [Fact]
    public void Remaining_FollowsTheClock()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(25);

        Assert.Equal(TimeSpan.FromSeconds(35), countdown.Remaining);
        Assert.False(countdown.Finished);
    }

    [Fact]
    public void Pause_FreezesTheCountWhereItIs()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(25);
        countdown.Pause();

        now = Start.AddSeconds(90);

        Assert.True(countdown.Paused);
        Assert.Equal(TimeSpan.FromSeconds(35), countdown.Remaining);
        Assert.False(countdown.Finished);
    }

    [Fact]
    public void Resume_ContinuesFromWhereItStopped()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(25);
        countdown.Pause();
        now = Start.AddSeconds(90);
        countdown.Resume();

        // The count continues from the 35 seconds that were left, not from the full minute.
        Assert.Equal(TimeSpan.FromSeconds(35), countdown.Remaining);
        Assert.False(countdown.Paused);

        now = Start.AddSeconds(100);

        Assert.Equal(TimeSpan.FromSeconds(25), countdown.Remaining);
    }

    [Fact]
    public void TheCountNeverGoesBelowZero()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(120);

        Assert.Equal(TimeSpan.Zero, countdown.Remaining);
        Assert.True(countdown.Finished);
    }

    [Fact]
    public void Restart_BeginsTheCountOver()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(60);
        Assert.True(countdown.Finished);

        countdown.Restart();

        Assert.Equal(TimeSpan.FromSeconds(60), countdown.Remaining);
        Assert.False(countdown.Finished);
        Assert.False(countdown.Paused);
    }

    [Fact]
    public void Restart_WhilePaused_AlsoEndsThePause()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(10);
        countdown.Pause();
        countdown.Restart();

        Assert.False(countdown.Paused);
        Assert.Equal(TimeSpan.FromSeconds(60), countdown.Remaining);
    }

    [Fact]
    public void Rearm_PointsTheCountAtAnotherDuration()
    {
        var now = Start;
        var countdown = new RestCountdown(60, () => now);

        now = Start.AddSeconds(30);
        countdown.Rearm(120);

        Assert.Equal(120, countdown.Seconds);
        Assert.Equal(TimeSpan.FromSeconds(120), countdown.Remaining);
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(9, "00:09")]
    [InlineData(60, "01:00")]
    [InlineData(90, "01:30")]
    [InlineData(600, "10:00")]
    public void Format_ShowsMinutesAndSeconds(int seconds, string expected)
    {
        Assert.Equal(expected, RestCountdown.Format(TimeSpan.FromSeconds(seconds)));
    }
}

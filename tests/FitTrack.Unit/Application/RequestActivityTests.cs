using FitTrack.UI.Notifications;

namespace FitTrack.Unit.Application;

/// <summary>
/// The counter behind the overlay that blocks the screen while the server answers: it counts the
/// requests in flight and never lets the screen through while one of them is on its way.
/// </summary>
public sealed class RequestActivityTests
{
    [Fact]
    public void Track_KeepsTheScreenBlockedUntilEveryRequestEnds()
    {
        var activity = new RequestActivity();

        Assert.False(activity.IsBusy);

        var first = activity.Track();
        var second = activity.Track();

        Assert.True(activity.IsBusy);
        Assert.Equal(2, activity.Pending);

        first.Dispose();

        Assert.True(activity.IsBusy);

        second.Dispose();

        Assert.False(activity.IsBusy);
        Assert.Equal(0, activity.Pending);
    }

    [Fact]
    public void Track_ReleasedTwice_DoesNotOpenTheScreenWhileARequestIsOnItsWay()
    {
        var activity = new RequestActivity();
        var scope = activity.Track();

        scope.Dispose();
        scope.Dispose();

        Assert.Equal(0, activity.Pending);
        Assert.False(activity.IsBusy);
    }

    [Fact]
    public void Track_TellsTheHostWhenTheStateChanges()
    {
        var activity = new RequestActivity();
        var changes = 0;
        activity.Changed += () => changes++;

        var scope = activity.Track();
        scope.Dispose();

        Assert.Equal(2, changes);
    }
}

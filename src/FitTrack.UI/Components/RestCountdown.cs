namespace FitTrack.UI.Components;

/// <summary>
/// The countdown of the rest between sets: how much is left, the pause, the resume and the end of
/// the count. It is a plain class with an injected clock, so the behaviour is testable without
/// waiting for the wall clock and the modal only has to draw it. The count is the client's own
/// state: it never crosses HTTP and it is discarded when the modal closes.
/// </summary>
public sealed class RestCountdown
{
    private readonly Func<DateTimeOffset> _now;
    private DateTimeOffset _endsAt;
    private TimeSpan _pausedRemaining;
    private bool _paused;

    public RestCountdown(int seconds, Func<DateTimeOffset> now)
    {
        _now = now;
        Seconds = seconds;
        _endsAt = now() + TimeSpan.FromSeconds(seconds);
    }

    /// <summary>The prescribed duration, in seconds: the count restarts from it.</summary>
    public int Seconds { get; private set; }

    public bool Paused => _paused;

    /// <summary>How much is left; it never goes below zero.</summary>
    public TimeSpan Remaining => _paused
        ? _pausedRemaining
        : Max(_endsAt - _now(), TimeSpan.Zero);

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;

    /// <summary>The count is over — the alert is up and the athlete can restart it.</summary>
    public bool Finished => Remaining == TimeSpan.Zero;

    /// <summary>
    /// Freezes the count where it is. The remaining time is read before the flag flips, otherwise the
    /// getter would answer the paused value — still zero — and the rest would die on the pause.
    /// </summary>
    public void Pause()
    {
        if (_paused || Finished)
        {
            return;
        }

        _pausedRemaining = Remaining;
        _paused = true;
    }

    /// <summary>Continues the count from where it was paused, not from the full duration.</summary>
    public void Resume()
    {
        if (!_paused)
        {
            return;
        }

        _paused = false;
        _endsAt = _now() + _pausedRemaining;
    }

    /// <summary>Starts the count over from the prescribed duration.</summary>
    public void Restart()
    {
        _paused = false;
        _pausedRemaining = TimeSpan.Zero;
        _endsAt = _now() + TimeSpan.FromSeconds(Seconds);
    }

    /// <summary>
    /// Points the count at another duration — the modal rearms it when the exercise behind the
    /// timer changes, so the title and the count never disagree.
    /// </summary>
    public void Rearm(int seconds)
    {
        Seconds = seconds;
        Restart();
    }

    /// <summary>The count as the athlete reads it: minutes and seconds, both padded.</summary>
    public static string Format(TimeSpan value) =>
        $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
}

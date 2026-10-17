using FitTrack.Application.Ports.Out;

namespace FitTrack.Unit.Fakes;

/// <summary>Clock fake that always returns the instant supplied by the test.</summary>
internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

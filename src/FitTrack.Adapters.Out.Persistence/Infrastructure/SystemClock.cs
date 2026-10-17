using FitTrack.Application.Ports.Out;

namespace FitTrack.Adapters.Out.Persistence.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

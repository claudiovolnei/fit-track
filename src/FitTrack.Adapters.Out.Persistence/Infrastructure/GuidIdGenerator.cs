using FitTrack.Application.Ports.Out;

namespace FitTrack.Adapters.Out.Persistence.Infrastructure;

internal sealed class GuidIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}

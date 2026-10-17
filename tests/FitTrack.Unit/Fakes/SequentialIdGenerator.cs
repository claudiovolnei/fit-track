using FitTrack.Application.Ports.Out;

namespace FitTrack.Unit.Fakes;

/// <summary>Deterministic id generator: successive calls never collide.</summary>
internal sealed class SequentialIdGenerator : IIdGenerator
{
    private int _next;

    public Guid NewId() => new(++_next, 0, 0, new byte[8]);
}

namespace FitTrack.Application.Ports.Out;

/// <summary>Generates the identifiers used by newly created aggregates.</summary>
public interface IIdGenerator
{
    Guid NewId();
}

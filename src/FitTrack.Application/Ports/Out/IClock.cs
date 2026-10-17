namespace FitTrack.Application.Ports.Out;

/// <summary>Supplies the current instant so no use case reads the machine clock directly.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

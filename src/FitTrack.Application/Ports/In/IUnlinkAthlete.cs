using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for undoing the roster link of one athlete.</summary>
public interface IUnlinkAthlete
{
    Task ExecuteAsync(UnlinkAthleteCommand command, CancellationToken cancellationToken);
}

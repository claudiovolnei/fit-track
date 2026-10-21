using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for linking one athlete to one personal trainer.</summary>
public interface ILinkAthlete
{
    /// <summary>Idempotent when the athlete is already linked to the same trainer.</summary>
    Task ExecuteAsync(LinkAthleteCommand command, CancellationToken cancellationToken);
}

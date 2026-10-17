using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the athletes a plan can be assigned to.</summary>
public interface IListAthletes
{
    Task<IReadOnlyList<AthleteDto>> ExecuteAsync(CancellationToken cancellationToken);
}

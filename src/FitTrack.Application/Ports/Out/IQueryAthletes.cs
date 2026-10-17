using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the accounts the trainer can assign a plan to.</summary>
public interface IQueryAthletes
{
    /// <summary>Accounts holding the athlete profile, telling whether each already has a plan.</summary>
    Task<IReadOnlyList<AthleteDto>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken);
}

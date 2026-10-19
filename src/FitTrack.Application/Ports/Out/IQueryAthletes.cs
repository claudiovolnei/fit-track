using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the accounts the trainer can assign a series to.</summary>
public interface IQueryAthletes
{
    /// <summary>
    /// Accounts holding the athlete profile, telling which series each is following and how many days
    /// of the term are left. <paramref name="now"/> fixes the instant the days are counted at.
    /// </summary>
    Task<IReadOnlyList<AthleteDto>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken);
}

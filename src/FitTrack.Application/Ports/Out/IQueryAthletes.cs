using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the accounts the roster links to the personal trainers.</summary>
public interface IQueryAthletes
{
    /// <summary>
    /// Accounts holding the athlete profile, with the series each one follows and how many days
    /// of the term are left. <paramref name="now"/> fixes the instant the days are counted at.
    /// The administrator reads the whole roster, so the answer also carries the trainer's e-mail.
    /// </summary>
    Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken);

    /// <summary>Only the athletes linked to <paramref name="trainerId"/>: the trainer's roster.</summary>
    Task<IReadOnlyList<AthleteDto>> ListAsync(
        DateTimeOffset now,
        UserId trainerId,
        CancellationToken cancellationToken);

    /// <summary>A page of the trainer's roster.</summary>
    Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        UserId trainerId,
        CancellationToken cancellationToken);

    /// <summary>Athletes still linked to nobody: the candidates the link screen offers.</summary>
    Task<PagedResult<AthleteDto>> ListUnlinkedAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Whether the account holds the personal trainer profile, so a link can name it as target.</summary>
    Task<bool> ExistsPersonalTrainerAsync(UserId userId, CancellationToken cancellationToken);
}

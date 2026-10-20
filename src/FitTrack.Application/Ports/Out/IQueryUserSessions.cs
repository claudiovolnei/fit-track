using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the current user's workout history.</summary>
public interface IQueryUserSessions
{
    Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// One page of the history, filtered by the workout name, with the volume comparison against the
    /// current prescription and a summary of every filtered session.
    /// </summary>
    Task<WorkoutHistoryDto> ListByUserAsync(
        UserId userId,
        ListQuery query,
        CancellationToken cancellationToken);
}

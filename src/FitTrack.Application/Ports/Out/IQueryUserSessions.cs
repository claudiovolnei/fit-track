using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the current user's workout history.</summary>
public interface IQueryUserSessions
{
    Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// The same page, restricted to the accounts holding <paramref name="role"/>, when named.
    /// </summary>
    Task<WorkoutHistoryDto> ListByUserAsync(
        UserId userId,
        ListQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Whether the athlete still has a workout open on that date. A workout already started can
    /// always be finished, and the athlete only reaches it through the series: a rule that takes the
    /// series away has to know about it first.
    /// </summary>
    Task<bool> HasRunningSessionAsync(
        UserId userId,
        DateOnly workoutDate,
        CancellationToken cancellationToken);
}

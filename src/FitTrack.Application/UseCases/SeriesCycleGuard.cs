using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>
/// The rule the ways of closing a cycle share. A workout already started can always be finished,
/// and the athlete only reaches it through the series: closing the cycle while it runs would strand
/// the session.
/// </summary>
internal static class SeriesCycleGuard
{
    public static async Task EnsureNothingRunningAsync(
        IQueryUserSessions sessions,
        UserId athleteId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (await sessions.HasRunningSessionAsync(athleteId, today, cancellationToken))
        {
            throw new ConflictException(
                "workout_series.assignment.running",
                "The athlete has a workout in progress.");
        }
    }
}

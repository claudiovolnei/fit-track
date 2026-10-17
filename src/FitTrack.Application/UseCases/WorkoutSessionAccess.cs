using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// The access rule of the execution use cases: an athlete only reaches their own sessions, and a
/// session that is not theirs answers as if it did not exist.
/// </summary>
internal static class WorkoutSessionAccess
{
    public static async Task<WorkoutSession> RequireOwnedAsync(
        IWorkoutSessionRepository sessions,
        Guid sessionId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(new WorkoutSessionId(sessionId), cancellationToken);

        if (session is null || session.UserId != userId)
        {
            throw new NotFoundException($"Workout session '{sessionId}' was not found.");
        }

        return session;
    }
}

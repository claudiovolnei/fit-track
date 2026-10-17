using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the current user's workout history.</summary>
public interface IQueryUserSessions
{
    Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);
}

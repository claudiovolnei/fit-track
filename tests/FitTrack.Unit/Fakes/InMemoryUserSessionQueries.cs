using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for a user's workout history.</summary>
internal sealed class InMemoryUserSessionQueries : IQueryUserSessions
{
    private readonly List<(UserId UserId, WorkoutSessionDto Session)> _sessions = [];

    public void Seed(UserId userId, WorkoutSessionDto session) => _sessions.Add((userId, session));

    public Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSessionDto>>(
            _sessions.Where(entry => entry.UserId == userId).Select(entry => entry.Session).ToList());
}

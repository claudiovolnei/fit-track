using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>In-memory write repository honouring the <see cref="IWorkoutSessionRepository"/> contract.</summary>
internal sealed class InMemoryWorkoutSessionRepository : IWorkoutSessionRepository
{
    private readonly Dictionary<WorkoutSessionId, WorkoutSession> _byId = [];

    public IReadOnlyCollection<WorkoutSession> All => _byId.Values;

    public Task<WorkoutSession?> GetByIdAsync(WorkoutSessionId id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var session) ? session : null);

    public Task<WorkoutSession?> GetByPlannedWorkoutAsync(
        UserId userId,
        DateOnly workoutDate,
        WorkoutSeriesWorkoutId workoutId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(session =>
            session.UserId == userId
            && session.WorkoutDate.Value == workoutDate
            && session.PlannedWorkoutId == workoutId));

    public Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        _byId[session.Id] = session;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        _byId.Remove(session.Id);

        return Task.CompletedTask;
    }
}

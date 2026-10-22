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

    public Task<IReadOnlyList<WorkoutSession>> ListByDateAsync(
        UserId userId,
        DateOnly workoutDate,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSession>>(
            [.. _byId.Values.Where(session =>
                session.UserId == userId && session.WorkoutDate.Value == workoutDate)]);

    public Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSession>>(
            [.. _byId.Values.Where(session =>
                session.FinishedAt is null && session.StartedAt <= cutoff)]);

    public Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        UserId userId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSession>>(
            [.. _byId.Values.Where(session =>
                session.UserId == userId && session.FinishedAt is null && session.StartedAt <= cutoff)]);

    public Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        if (_byId.TryGetValue(session.Id, out var existing) && existing.FinishedAt is not null)
        {
            return Task.CompletedTask;
        }

        _byId[session.Id] = session;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        if (_byId.TryGetValue(session.Id, out var existing) && existing.FinishedAt is not null)
        {
            return Task.CompletedTask;
        }

        _byId.Remove(session.Id);

        return Task.CompletedTask;
    }
}

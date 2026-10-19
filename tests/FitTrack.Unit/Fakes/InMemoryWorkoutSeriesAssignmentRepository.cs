using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>
/// In-memory write repository honouring the <see cref="IWorkoutSeriesAssignmentRepository"/> contract.
/// One assignment per athlete, keyed by the athlete, the same way the unique index keeps it.
/// </summary>
internal sealed class InMemoryWorkoutSeriesAssignmentRepository : IWorkoutSeriesAssignmentRepository
{
    private readonly Dictionary<UserId, WorkoutSeriesAssignment> _byUser = [];

    public IReadOnlyCollection<WorkoutSeriesAssignment> All => _byUser.Values;

    public Task<WorkoutSeriesAssignment?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_byUser.TryGetValue(userId, out var assignment) ? assignment : null);

    public Task SaveAsync(WorkoutSeriesAssignment assignment, CancellationToken cancellationToken)
    {
        _byUser[assignment.UserId] = assignment;

        return Task.CompletedTask;
    }
}

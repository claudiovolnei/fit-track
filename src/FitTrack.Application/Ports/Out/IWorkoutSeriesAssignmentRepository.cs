using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="WorkoutSeriesAssignment"/> aggregate. One athlete holds at most
/// one assignment, so <see cref="GetByUserIdAsync"/> answers the single-athlete questions; the batch
/// lookup serves the flows that prescribe one series to several athletes at once.
/// </summary>
public interface IWorkoutSeriesAssignmentRepository
{
    Task<WorkoutSeriesAssignment?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkoutSeriesAssignment>> GetByUserIdsAsync(
        IReadOnlyList<UserId> userIds,
        CancellationToken cancellationToken);

    Task SaveAsync(WorkoutSeriesAssignment assignment, CancellationToken cancellationToken);
}

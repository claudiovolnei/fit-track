using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="WorkoutSeriesAssignment"/> aggregate. One athlete holds at most
/// one assignment, so <see cref="GetByUserIdAsync"/> is the whole lookup the use cases need.
/// </summary>
public interface IWorkoutSeriesAssignmentRepository
{
    Task<WorkoutSeriesAssignment?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    Task SaveAsync(WorkoutSeriesAssignment assignment, CancellationToken cancellationToken);
}

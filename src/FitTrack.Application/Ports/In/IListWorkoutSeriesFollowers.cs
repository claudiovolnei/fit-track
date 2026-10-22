using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>
/// Reads the athletes a series serves: the assignment is the link, so a series shared by a roster
/// lists every athlete on it, open cycle or closed.
/// </summary>
public interface IListWorkoutSeriesFollowers
{
    Task<IReadOnlyList<WorkoutSeriesFollowerDto>> ExecuteAsync(Guid seriesId, CancellationToken cancellationToken);
}

using FitTrack.Application.Dtos;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Read-side port for the series screens. Every projection orders the workouts by position — the cut-1
/// lesson was ordering by a random key and returning a scrambled workout.
/// </summary>
public interface IQueryWorkoutSeries
{
    Task<IReadOnlyList<WorkoutSeriesDto>> ListByTrainerAsync(UserId trainerId, CancellationToken cancellationToken);

    Task<WorkoutSeriesDto?> GetByIdAsync(WorkoutSeriesId seriesId, CancellationToken cancellationToken);

    /// <summary>
    /// The series the athlete is following, with the state of <paramref name="today"/> per workout, or
    /// null when nothing is assigned. <paramref name="now"/> comes from the clock port so the elapsed
    /// seconds and the days remaining are computed at a known instant.
    /// </summary>
    Task<MyWorkoutSeriesDto?> GetForAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="WorkoutSeries"/> aggregate. One repository per aggregate root.
/// </summary>
public interface IWorkoutSeriesRepository
{
    Task<WorkoutSeries?> GetByIdAsync(WorkoutSeriesId id, CancellationToken cancellationToken);

    /// <summary>
    /// The workout with its prescription, wherever it lives. A session keeps the chosen workout by id
    /// and no foreign key, so the rule that a set only lands on a prescribed exercise is checked here,
    /// across aggregates, before the session changes.
    /// </summary>
    Task<WorkoutSeriesWorkout?> GetWorkoutAsync(WorkoutSeriesWorkoutId id, CancellationToken cancellationToken);

    /// <summary>
    /// Tells whether the trainer already authored a series with that name. The unique index is the
    /// backstop under concurrency; this answers first so the caller gets a stated rule instead of a
    /// raw error. <paramref name="excluding"/> lets a rename keep its own name.
    /// </summary>
    Task<bool> NameTakenAsync(
        WorkoutSeriesName name,
        UserId trainerId,
        WorkoutSeriesId? excluding,
        CancellationToken cancellationToken);

    Task SaveAsync(WorkoutSeries series, CancellationToken cancellationToken);
}

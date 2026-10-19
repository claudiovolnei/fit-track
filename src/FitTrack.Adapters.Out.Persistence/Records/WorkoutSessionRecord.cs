namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for a workout session. Deliberately separate from the aggregate.</summary>
public sealed class WorkoutSessionRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly WorkoutDate { get; set; }

    /// <summary>
    /// Workout of the series the workout came from. No foreign key on purpose: the series lives in
    /// another context, and removing a workout must never rewrite or erase someone's history.
    /// </summary>
    public Guid? WorkoutSeriesWorkoutId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Null while the workout is still running.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    public List<PerformedSetRecord> Sets { get; set; } = [];
}

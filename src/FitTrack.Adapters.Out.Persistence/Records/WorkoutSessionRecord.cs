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

    /// <summary>
    /// How intense the athlete said the workout was, as the numeric value of the domain enum. Null
    /// for the sessions finished before the athlete started answering it.
    /// </summary>
    public int? Intensity { get; set; }

    /// <summary>What the athlete wrote about the workout. Null when they wrote nothing.</summary>
    public string? Notes { get; set; }

    public List<PerformedSetRecord> Sets { get; set; } = [];

    /// <summary>The exercises the athlete ticked as done.</summary>
    public List<CompletedExerciseRecord> CompletedExercises { get; set; } = [];
}

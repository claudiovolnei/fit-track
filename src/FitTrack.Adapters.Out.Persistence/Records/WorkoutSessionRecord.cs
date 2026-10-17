namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for a workout session. Deliberately separate from the aggregate.</summary>
public sealed class WorkoutSessionRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly WorkoutDate { get; set; }

    /// <summary>
    /// Option of the plan the workout came from. No foreign key on purpose: the plan lives in another
    /// context, and removing an option must never rewrite or erase someone's history.
    /// </summary>
    public Guid? WorkoutPlanOptionId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Null while the workout is still running.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    public List<PerformedSetRecord> Sets { get; set; } = [];
}

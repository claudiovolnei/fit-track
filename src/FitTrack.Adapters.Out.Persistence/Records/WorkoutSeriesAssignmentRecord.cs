namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for the link between one athlete and the one series they follow. The unique
/// index on <see cref="UserId"/> is what the "one series per athlete" rule rests on; the athlete and
/// the author are accounts of the Identity context, so they stay as ids.
/// </summary>
public sealed class WorkoutSeriesAssignmentRecord
{
    public Guid Id { get; set; }

    public Guid WorkoutSeriesId { get; set; }

    public Guid UserId { get; set; }

    public Guid TrainerId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>
    /// Stored as assigned + the fixed term so the read side and the expiry index use one column. The
    /// domain is the one that computes it; this is a projection, not a second rule.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

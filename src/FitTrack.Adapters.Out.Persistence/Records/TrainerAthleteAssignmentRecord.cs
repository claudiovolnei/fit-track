namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for the roster link between one athlete and one personal trainer. The unique
/// index on <see cref="AthleteId"/> is what the "one trainer per athlete" rule rests on; both sides
/// are accounts of the Identity context, so they stay as ids.
/// </summary>
public sealed class TrainerAthleteAssignmentRecord
{
    public Guid Id { get; set; }

    public Guid TrainerId { get; set; }

    public Guid AthleteId { get; set; }

    public DateTimeOffset LinkedAt { get; set; }
}

using FitTrack.Domain.Common;

namespace FitTrack.Domain.Identity;

/// <summary>
/// The roster link between one athlete profile and one personal trainer profile. One athlete answers
/// to a single trainer, so the unique index on the athlete in the persistence is the rule; undoing the
/// link removes the whole aggregate from the store, the same way a session is cancelled.
/// </summary>
public sealed class TrainerAthleteAssignment
{
    private TrainerAthleteAssignment(
        TrainerAthleteAssignmentId id,
        UserId trainerId,
        UserId athleteId,
        DateTimeOffset linkedAt)
    {
        Id = id;
        TrainerId = trainerId;
        AthleteId = athleteId;
        LinkedAt = linkedAt;
    }

    public TrainerAthleteAssignmentId Id { get; }

    /// <summary>The personal trainer the athlete answers to.</summary>
    public UserId TrainerId { get; }

    /// <summary>The athlete on the trainer's roster.</summary>
    public UserId AthleteId { get; }

    public DateTimeOffset LinkedAt { get; }

    public static TrainerAthleteAssignment Link(
        TrainerAthleteAssignmentId id,
        UserId trainerId,
        UserId athleteId,
        DateTimeOffset linkedAt)
    {
        if (trainerId.Value == Guid.Empty)
        {
            throw new DomainException(
                "roster.trainer.required",
                "A roster link must name a personal trainer.");
        }

        if (athleteId.Value == Guid.Empty)
        {
            throw new DomainException(
                "roster.athlete.required",
                "A roster link must name an athlete.");
        }

        return new TrainerAthleteAssignment(id, trainerId, athleteId, linkedAt);
    }
}

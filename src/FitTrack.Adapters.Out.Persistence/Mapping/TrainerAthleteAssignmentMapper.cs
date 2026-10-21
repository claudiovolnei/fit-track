using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>Translates between the <see cref="TrainerAthleteAssignment"/> aggregate and its record.</summary>
internal static class TrainerAthleteAssignmentMapper
{
    /// <summary>Rehydrates through <see cref="TrainerAthleteAssignment.Link"/>, which states the same invariants.</summary>
    public static TrainerAthleteAssignment ToDomain(TrainerAthleteAssignmentRecord record) =>
        TrainerAthleteAssignment.Link(
            new TrainerAthleteAssignmentId(record.Id),
            new UserId(record.TrainerId),
            new UserId(record.AthleteId),
            record.LinkedAt);

    public static TrainerAthleteAssignmentRecord ToRecord(TrainerAthleteAssignment assignment) => new()
    {
        Id = assignment.Id.Value,
        TrainerId = assignment.TrainerId.Value,
        AthleteId = assignment.AthleteId.Value,
        LinkedAt = assignment.LinkedAt,
    };
}

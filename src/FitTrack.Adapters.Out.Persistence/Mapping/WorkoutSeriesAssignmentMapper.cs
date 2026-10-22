using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>Translates between the <see cref="WorkoutSeriesAssignment"/> aggregate and its record.</summary>
internal static class WorkoutSeriesAssignmentMapper
{
    /// <summary>
    /// Rehydrates through <see cref="WorkoutSeriesAssignment.Assign"/>, which recomputes the term from
    /// the stored <c>AssignedAt</c>: the 60-day rule keeps a single implementation instead of a second
    /// copy that could drift from the column. The stored <c>ExpiresAt</c> exists for the queries and
    /// the expiry index. A cycle closed early is rehydrated through
    /// <see cref="WorkoutSeriesAssignment.Close"/>, so the stored instant is the one the rule keeps.
    /// </summary>
    public static WorkoutSeriesAssignment ToDomain(WorkoutSeriesAssignmentRecord record)
    {
        var assignment = WorkoutSeriesAssignment.Assign(
            new WorkoutSeriesAssignmentId(record.Id),
            new WorkoutSeriesId(record.WorkoutSeriesId),
            new UserId(record.UserId),
            new UserId(record.TrainerId),
            record.AssignedAt);

        if (record.ClosedAt is { } closedAt)
        {
            assignment.Close(closedAt);
        }

        return assignment;
    }

    public static WorkoutSeriesAssignmentRecord ToRecord(WorkoutSeriesAssignment assignment) => new()
    {
        Id = assignment.Id.Value,
        WorkoutSeriesId = assignment.SeriesId.Value,
        UserId = assignment.UserId.Value,
        TrainerId = assignment.TrainerId.Value,
        AssignedAt = assignment.AssignedAt,
        ExpiresAt = assignment.ExpiresAt,
        ClosedAt = assignment.ClosedAt,
    };

    public static void CopyInto(WorkoutSeriesAssignment assignment, WorkoutSeriesAssignmentRecord record)
    {
        record.WorkoutSeriesId = assignment.SeriesId.Value;
        record.TrainerId = assignment.TrainerId.Value;
        record.AssignedAt = assignment.AssignedAt;
        record.ExpiresAt = assignment.ExpiresAt;
        record.ClosedAt = assignment.ClosedAt;
    }
}

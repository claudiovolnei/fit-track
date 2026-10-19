using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// The link between one athlete and the one series they are following. It is an aggregate of its own
/// because the series is a reusable template: the same series is assigned to many athletes, each with
/// their own term. The term is fixed at <see cref="TermInDays"/> days, counted from the assignment.
/// </summary>
public sealed class WorkoutSeriesAssignment
{
    public const int TermInDays = 60;

    private WorkoutSeriesAssignment(
        WorkoutSeriesAssignmentId id,
        WorkoutSeriesId seriesId,
        UserId userId,
        UserId trainerId,
        DateTimeOffset assignedAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        SeriesId = seriesId;
        UserId = userId;
        TrainerId = trainerId;
        AssignedAt = assignedAt;
        ExpiresAt = expiresAt;
    }

    public WorkoutSeriesAssignmentId Id { get; }

    public WorkoutSeriesId SeriesId { get; private set; }

    /// <summary>The athlete the series is assigned to.</summary>
    public UserId UserId { get; }

    /// <summary>The trainer who assigned it.</summary>
    public UserId TrainerId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>The instant the term ends. From here on the athlete can no longer start a new workout.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    public static WorkoutSeriesAssignment Assign(
        WorkoutSeriesAssignmentId id,
        WorkoutSeriesId seriesId,
        UserId userId,
        UserId trainerId,
        DateTimeOffset assignedAt)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new DomainException(
                "workout_series.assignment.athlete.required",
                "A workout series must be assigned to an athlete.");
        }

        if (trainerId.Value == Guid.Empty)
        {
            throw new DomainException("workout_series.trainer.required", "A workout series must have an author.");
        }

        return new WorkoutSeriesAssignment(
            id,
            seriesId,
            userId,
            trainerId,
            assignedAt,
            assignedAt.AddDays(TermInDays));
    }

    /// <summary>Points the athlete at another series and restarts the term from the new assignment.</summary>
    public void Reassign(WorkoutSeriesId seriesId, UserId trainerId, DateTimeOffset assignedAt)
    {
        SeriesId = seriesId;
        TrainerId = trainerId;
        AssignedAt = assignedAt;
        ExpiresAt = assignedAt.AddDays(TermInDays);
    }

    /// <summary>True from the instant the term ends on. Evaluated against the instant the caller passes.</summary>
    public bool IsExpiredAt(DateTimeOffset now) => IsExpired(ExpiresAt, now);

    /// <summary>Whole days left before the term ends, or zero once expired.</summary>
    public int DaysRemaining(DateTimeOffset now) => DaysRemainingUntil(ExpiresAt, now);

    /// <summary>
    /// The term rule without an aggregate instance, so the read side (which holds only the stored
    /// <c>ExpiresAt</c>) can apply the same arithmetic instead of a copy that could drift.
    /// </summary>
    public static bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset now) => now >= expiresAt;

    public static int DaysRemainingUntil(DateTimeOffset expiresAt, DateTimeOffset now) =>
        IsExpired(expiresAt, now) ? 0 : (int)Math.Ceiling((expiresAt - now).TotalDays);
}

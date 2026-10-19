using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// Execution aggregate root: one user's workout session on one date, holding the clock the athlete
/// started and finished it. Sets are registered through <see cref="RegisterSet"/> so no invalid set
/// can enter the aggregate — and none can enter once the workout is finished.
/// </summary>
public sealed class WorkoutSession
{
    private readonly List<PerformedSet> _performedSets = [];

    private WorkoutSession(
        WorkoutSessionId id,
        UserId userId,
        WorkoutDate workoutDate,
        DateTimeOffset startedAt,
        WorkoutSeriesWorkoutId? plannedWorkoutId)
    {
        Id = id;
        UserId = userId;
        WorkoutDate = workoutDate;
        StartedAt = startedAt;
        PlannedWorkoutId = plannedWorkoutId;
    }

    public WorkoutSessionId Id { get; }

    public UserId UserId { get; }

    public WorkoutDate WorkoutDate { get; }

    /// <summary>Instant the athlete started the workout. The timer is measured from here.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Null while the workout is still running.</summary>
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>
    /// Workout of the series this session came from. Kept as an id on purpose: the series lives in
    /// another context, and removing a workout must never rewrite what someone already did.
    /// </summary>
    public WorkoutSeriesWorkoutId? PlannedWorkoutId { get; }

    public IReadOnlyList<PerformedSet> PerformedSets => _performedSets;

    /// <summary>True until the athlete finishes the workout.</summary>
    public bool IsRunning => FinishedAt is null;

    public bool HasSets => _performedSets.Count > 0;

    public static WorkoutSession Start(
        WorkoutSessionId id,
        UserId userId,
        WorkoutDate workoutDate,
        DateTimeOffset startedAt) =>
        Create(id, userId, workoutDate, startedAt, plannedWorkoutId: null);

    public static WorkoutSession StartFromPlannedWorkout(
        WorkoutSessionId id,
        UserId userId,
        WorkoutDate workoutDate,
        DateTimeOffset startedAt,
        WorkoutSeriesWorkoutId plannedWorkoutId) =>
        Create(id, userId, workoutDate, startedAt, plannedWorkoutId);

    /// <summary>
    /// Elapsed time: frozen at <see cref="FinishedAt"/> once finished, still running until then.
    /// It takes the instant instead of reading a clock, so the aggregate stays pure.
    /// </summary>
    public TimeSpan Duration(DateTimeOffset now) => (FinishedAt ?? now) - StartedAt;

    public PerformedSet RegisterSet(ExerciseId exerciseId, Load load, Repetitions repetitions)
    {
        if (!IsRunning)
        {
            throw new DomainException("session.set.after_finish", "A finished workout cannot take more sets.");
        }

        var set = new PerformedSet(exerciseId, load, repetitions);
        _performedSets.Add(set);

        return set;
    }

    public void Finish(DateTimeOffset finishedAt)
    {
        if (!IsRunning)
        {
            throw new DomainException("session.finish.already_finished", "This workout is already finished.");
        }

        if (finishedAt < StartedAt)
        {
            throw new DomainException("session.finish.before_start", "A workout cannot finish before it started.");
        }

        FinishedAt = finishedAt;
    }

    private static WorkoutSession Create(
        WorkoutSessionId id,
        UserId userId,
        WorkoutDate workoutDate,
        DateTimeOffset startedAt,
        WorkoutSeriesWorkoutId? plannedWorkoutId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new DomainException("session.user.required", "A workout session must belong to a user.");
        }

        return new WorkoutSession(id, userId, workoutDate, startedAt, plannedWorkoutId);
    }
}

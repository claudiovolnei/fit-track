using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// Execution aggregate root: one user's workout session on one date, holding the clock the athlete
/// started and finished it. Sets are registered through <see cref="RegisterSet"/> so no invalid set
/// can enter the aggregate — and none can enter once the workout is finished. The exercises the
/// athlete ticked are kept here too, so the screen can show what is still missing before the workout
/// is closed.
/// </summary>
public sealed class WorkoutSession
{
    private readonly List<PerformedSet> _performedSets = [];

    private readonly List<ExerciseId> _completedExercises = [];

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

    /// <summary>
    /// The exercises the athlete ticked as done, in the order they were ticked. Registering a set
    /// ticks its exercise: performing a series of an exercise means the exercise was done.
    /// </summary>
    public IReadOnlyList<ExerciseId> CompletedExercises => _completedExercises;

    /// <summary>True until the athlete finishes the workout.</summary>
    public bool IsRunning => FinishedAt is null;

    public bool HasSets => _performedSets.Count > 0;

    /// <summary>Whether the athlete already ticked this exercise as done.</summary>
    public bool IsExerciseCompleted(ExerciseId exerciseId) => _completedExercises.Contains(exerciseId);

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

        // Performing a set of an exercise means the exercise was done: the tick follows the effort.
        Tick(exerciseId);

        return set;
    }

    /// <summary>
    /// Ticks an exercise as done. The athlete may do an exercise without registering its load, so the
    /// tick stands on its own.
    /// </summary>
    public void MarkExerciseCompleted(ExerciseId exerciseId)
    {
        RequireRunning();

        Tick(exerciseId);
    }

    /// <summary>
    /// Unticks an exercise. The sets already registered stay: what the athlete did is a fact, and the
    /// tick only says the exercise of the prescription is done.
    /// </summary>
    public void UnmarkExerciseCompleted(ExerciseId exerciseId)
    {
        RequireRunning();

        _completedExercises.Remove(exerciseId);
    }

    private void Tick(ExerciseId exerciseId)
    {
        if (exerciseId.Value == Guid.Empty)
        {
            throw new DomainException("set.exercise.required", "A performed set must reference an exercise.");
        }

        if (!_completedExercises.Contains(exerciseId))
        {
            _completedExercises.Add(exerciseId);
        }
    }

    private void RequireRunning()
    {
        if (!IsRunning)
        {
            throw new DomainException(
                "session.exercise.after_finish",
                "A finished workout cannot change which exercises are done.");
        }
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

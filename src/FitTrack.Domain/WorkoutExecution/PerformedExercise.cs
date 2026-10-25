using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// What the athlete did of one prescribed exercise: the effort of each performed set. The
/// aggregate turns it into the performed sets, so an invalid one never enters the session.
/// </summary>
public readonly record struct PerformedExercise
{
    /// <summary>An exercise the athlete did has at least one set.</summary>
    public const int MinSets = 1;

    public PerformedExercise(ExerciseId exerciseId, IReadOnlyList<(Load Load, Repetitions Repetitions)> sets)
    {
        if (exerciseId.Value == Guid.Empty)
        {
            throw new DomainException("set.exercise.required", "A performed set must reference an exercise.");
        }

        if (sets is null || sets.Count < MinSets)
        {
            throw new DomainException(
                "set.count.not_positive",
                "An exercise the athlete did must have at least one set.");
        }

        ExerciseId = exerciseId;
        Sets = sets;
    }

    public ExerciseId ExerciseId { get; }

    /// <summary>The effort of each performed set, in order.</summary>
    public IReadOnlyList<(Load Load, Repetitions Repetitions)> Sets { get; }
}
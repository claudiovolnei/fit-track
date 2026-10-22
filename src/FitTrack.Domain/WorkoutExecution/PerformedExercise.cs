using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// What the athlete did of one prescribed exercise: how many sets, how many repetitions each and
/// with which load. The aggregate turns it into the performed sets, so an invalid one never
/// enters the session.
/// </summary>
public readonly record struct PerformedExercise
{
    /// <summary>An exercise the athlete did has at least one set.</summary>
    public const int MinSets = 1;

    public PerformedExercise(ExerciseId exerciseId, Load load, Repetitions repetitions, int sets)
    {
        if (exerciseId.Value == Guid.Empty)
        {
            throw new DomainException("set.exercise.required", "A performed set must reference an exercise.");
        }

        if (sets < MinSets)
        {
            throw new DomainException(
                "set.count.not_positive",
                "An exercise the athlete did must have at least one set.");
        }

        ExerciseId = exerciseId;
        Load = load;
        Repetitions = repetitions;
        Sets = sets;
    }

    public ExerciseId ExerciseId { get; }

    public Load Load { get; }

    public Repetitions Repetitions { get; }

    /// <summary>How many sets of the exercise were performed.</summary>
    public int Sets { get; }
}

using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// A single set actually performed inside a <see cref="WorkoutSession"/> aggregate.
/// It is created only through the aggregate, which keeps the consistency boundary.
/// </summary>
public sealed class PerformedSet
{
    internal PerformedSet(ExerciseId exerciseId, Load load, Repetitions repetitions)
    {
        if (exerciseId.Value == Guid.Empty)
        {
            throw new DomainException("set.exercise.required", "A performed set must reference an exercise.");
        }

        ExerciseId = exerciseId;
        Load = load;
        Repetitions = repetitions;
    }

    public ExerciseId ExerciseId { get; }

    public Load Load { get; }

    public Repetitions Repetitions { get; }
}

using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// One specific workout of a series: a name and the exercises the trainer prescribes. It carries no
/// list of muscle groups of its own — the muscle groups are read from the exercises, so the catalog
/// stays the single place where the link lives.
/// </summary>
public sealed class WorkoutSeriesWorkout
{
    private readonly List<PrescribedExercise> _exercises;

    internal WorkoutSeriesWorkout(
        WorkoutSeriesWorkoutId id,
        int position,
        WorkoutName name,
        IReadOnlyList<PrescribedExercise> exercises)
    {
        Id = id;
        Position = position;
        Name = name;
        _exercises = [.. Ensure(exercises)];
    }

    public WorkoutSeriesWorkoutId Id { get; }

    /// <summary>One-based order inside the series, kept contiguous by the aggregate.</summary>
    public int Position { get; private set; }

    /// <summary>The letter the athlete reads on the sheet ("A".."E"), derived from <see cref="Position"/>.</summary>
    public string Label => LabelFor(Position);

    /// <summary>
    /// The letter of a one-based position. Exposed so the read side, which holds only the stored
    /// position, reads the same mapping instead of re-deriving the cast.
    /// </summary>
    public static string LabelFor(int position) => ((WorkoutLabel)position).ToString();

    public WorkoutName Name { get; private set; }

    public IReadOnlyList<PrescribedExercise> Exercises => _exercises;

    internal void Change(WorkoutName name, IReadOnlyList<PrescribedExercise> exercises)
    {
        var validated = Ensure(exercises);

        Name = name;
        _exercises.Clear();
        _exercises.AddRange(validated);
    }

    internal void MoveTo(int position) => Position = position;

    private static IReadOnlyList<PrescribedExercise> Ensure(IReadOnlyList<PrescribedExercise> exercises)
    {
        if (exercises.Count == 0)
        {
            throw new DomainException(
                "workout_series.workout.exercises.empty",
                "A workout must prescribe at least one exercise.");
        }

        if (exercises.Select(exercise => exercise.ExerciseId).Distinct().Count() != exercises.Count)
        {
            throw new DomainException(
                "workout_series.exercise.duplicate",
                "The same exercise cannot be prescribed twice in one workout.");
        }

        return exercises;
    }
}

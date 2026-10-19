namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for one prescribed exercise of a workout. The key is (workout, exercise): the
/// pair is the data, and a repeated pair is not a second row. <see cref="Position"/> keeps the
/// prescription order, which is part of the sheet the athlete reads.
/// </summary>
public sealed class WorkoutSeriesWorkoutExerciseRecord
{
    public Guid WorkoutSeriesWorkoutId { get; set; }

    public Guid ExerciseId { get; set; }

    public int Position { get; set; }

    public int Sets { get; set; }

    public int Repetitions { get; set; }

    /// <summary>Null for a bodyweight exercise: there is no weight to prescribe.</summary>
    public decimal? LoadKilograms { get; set; }
}

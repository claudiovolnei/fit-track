namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for one specific workout of a series, owned by a
/// <see cref="WorkoutSeriesRecord"/>.
/// </summary>
public sealed class WorkoutSeriesWorkoutRecord
{
    public Guid Id { get; set; }

    public Guid WorkoutSeriesId { get; set; }

    /// <summary>One-based order inside the series. The label (A..E) is read from it, never stored.</summary>
    public int Position { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<WorkoutSeriesWorkoutExerciseRecord> Exercises { get; set; } = [];
}

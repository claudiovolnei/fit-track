namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for a reusable workout series. Deliberately separate from the aggregate. The
/// table carries no athlete: the series is a template the trainer authors, and who follows it lives
/// in <see cref="WorkoutSeriesAssignmentRecord"/>.
/// </summary>
public sealed class WorkoutSeriesRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid TrainerId { get; set; }

    public List<WorkoutSeriesWorkoutRecord> Workouts { get; set; } = [];
}

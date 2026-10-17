namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for the exercise catalog. Deliberately separate from the aggregate.</summary>
public sealed class ExerciseRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid MuscleGroupId { get; set; }

    public bool IsActive { get; set; }
}

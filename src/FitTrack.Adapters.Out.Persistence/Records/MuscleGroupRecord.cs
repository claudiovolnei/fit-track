namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for a catalog muscle group. Deliberately separate from the aggregate.</summary>
public sealed class MuscleGroupRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for one option of a plan, owned by a <see cref="WorkoutPlanRecord"/>.</summary>
public sealed class WorkoutPlanOptionRecord
{
    public Guid Id { get; set; }

    public Guid WorkoutPlanId { get; set; }

    public int Position { get; set; }

    public List<WorkoutPlanOptionMuscleGroupRecord> MuscleGroups { get; set; } = [];
}

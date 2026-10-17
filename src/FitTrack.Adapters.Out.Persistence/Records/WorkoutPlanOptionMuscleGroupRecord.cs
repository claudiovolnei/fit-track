namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for the link between a plan option and one muscle group of the catalog. The key
/// is (option, muscle group): the pair is the data, and a repeated pair is not a second row.
/// </summary>
public sealed class WorkoutPlanOptionMuscleGroupRecord
{
    public Guid WorkoutPlanOptionId { get; set; }

    public Guid MuscleGroupId { get; set; }
}

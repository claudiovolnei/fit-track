namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>
/// Catalog aggregate root for a muscle group. The personal trainer maintains it, and both the
/// exercises and the training plan options point at it by id — that is what turns "which muscle
/// group" from a typed label into a link that cannot carry a typo.
/// </summary>
public sealed class MuscleGroup
{
    private MuscleGroup(MuscleGroupId id, MuscleGroupName name)
    {
        Id = id;
        Name = name;
    }

    public MuscleGroupId Id { get; }

    public MuscleGroupName Name { get; private set; }

    public static MuscleGroup Create(MuscleGroupId id, MuscleGroupName name) => new(id, name);

    public void Rename(MuscleGroupName name) => Name = name;
}

using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>
/// Catalog aggregate root. Every exercise belongs to a muscle group of the same context, so the
/// link is a real reference and not a repeated label. Mutating it requires the personal trainer
/// role, which the application layer enforces through the current-user port.
/// </summary>
public sealed class Exercise
{
    private Exercise(ExerciseId id, ExerciseName name, MuscleGroupId muscleGroupId, bool isActive)
    {
        Id = id;
        Name = name;
        MuscleGroupId = EnsureMuscleGroup(muscleGroupId);
        IsActive = isActive;
    }

    public ExerciseId Id { get; }

    public ExerciseName Name { get; private set; }

    public MuscleGroupId MuscleGroupId { get; private set; }

    public bool IsActive { get; private set; }

    public static Exercise Create(ExerciseId id, ExerciseName name, MuscleGroupId muscleGroupId) =>
        new(id, name, muscleGroupId, isActive: true);

    public void Rename(ExerciseName name) => Name = name;

    public void ChangeMuscleGroup(MuscleGroupId muscleGroupId) => MuscleGroupId = EnsureMuscleGroup(muscleGroupId);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static MuscleGroupId EnsureMuscleGroup(MuscleGroupId muscleGroupId) =>
        muscleGroupId.Value == Guid.Empty
            ? throw new DomainException(
                "exercise.muscle_group.required",
                "An exercise must belong to a muscle group.")
            : muscleGroupId;
}

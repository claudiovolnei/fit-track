using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="MuscleGroup"/> aggregate. One repository per aggregate root.
/// </summary>
public interface IMuscleGroupRepository
{
    Task<MuscleGroup?> GetByIdAsync(MuscleGroupId id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the groups a caller referenced by id. Used where a whole list arrives at once, as in the
    /// muscle groups of a plan option.
    /// </summary>
    Task<IReadOnlyList<MuscleGroup>> GetByIdsAsync(
        IReadOnlyList<MuscleGroupId> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tells whether another muscle group already carries the name. The unique index is the backstop
    /// under concurrency; this answers first so the caller gets a stated rule instead of a raw error.
    /// </summary>
    Task<bool> NameTakenAsync(
        MuscleGroupName name,
        MuscleGroupId? excluding,
        CancellationToken cancellationToken);

    Task SaveAsync(MuscleGroup muscleGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Drops the aggregate. The use case refuses deletion while exercises still belong to the
    /// group, so the raw removal only runs on an empty group.
    /// </summary>
    Task RemoveAsync(MuscleGroup muscleGroup, CancellationToken cancellationToken);
}

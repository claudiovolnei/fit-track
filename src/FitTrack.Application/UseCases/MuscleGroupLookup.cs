using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Resolves muscle groups the caller referenced by id. Shared by the use cases that link something to
/// the catalog, so an unknown reference is answered the same way everywhere.
/// </summary>
internal static class MuscleGroupLookup
{
    public static async Task<MuscleGroup> RequireAsync(
        IMuscleGroupRepository muscleGroups,
        Guid muscleGroupId,
        CancellationToken cancellationToken) =>
        await muscleGroups.GetByIdAsync(new MuscleGroupId(muscleGroupId), cancellationToken)
            ?? throw new NotFoundException($"Muscle group '{muscleGroupId}' was not found.");

    /// <summary>
    /// Validates a whole list at once and returns the names by id. An empty list is not refused here:
    /// the aggregate states that rule, and stating it twice would only make the message drift.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(
        IMuscleGroupRepository muscleGroups,
        IReadOnlyList<Guid> muscleGroupIds,
        CancellationToken cancellationToken)
    {
        if (muscleGroupIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = muscleGroupIds.Select(id => new MuscleGroupId(id)).ToArray();
        var found = await muscleGroups.GetByIdsAsync(ids, cancellationToken);
        var names = found.ToDictionary(muscleGroup => muscleGroup.Id.Value, muscleGroup => muscleGroup.Name.Value);

        var missing = ids.Where(id => !names.ContainsKey(id.Value)).Select(id => id.Value).ToArray();

        if (missing.Length > 0)
        {
            throw new NotFoundException($"Muscle group(s) '{string.Join("', '", missing)}' were not found.");
        }

        return names;
    }
}

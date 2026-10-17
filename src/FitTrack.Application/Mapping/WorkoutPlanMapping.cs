using FitTrack.Application.Dtos;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Mapping;

/// <summary>
/// Translates a <see cref="WorkoutPlanOption"/> into its read model. The aggregate only holds muscle
/// group ids, so the caller resolves the names once and passes the lookup in.
/// </summary>
internal static class WorkoutPlanMapping
{
    public static WorkoutPlanOptionDto ToDto(
        this WorkoutPlanOption option,
        IReadOnlyDictionary<Guid, string> muscleGroupNames) =>
        new(
            option.Id.Value,
            option.Position,
            [.. option.MuscleGroupIds.Select(id => new MuscleGroupDto(id.Value, muscleGroupNames[id.Value]))],
            Today: null);
}

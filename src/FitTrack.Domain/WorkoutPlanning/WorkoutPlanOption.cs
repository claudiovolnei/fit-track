using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// One option of a plan: the muscle groups the athlete may train together. It is deliberately
/// not bound to a weekday — the athlete picks the option on the day, which is what lets two
/// people train the same plan on different days.
/// </summary>
public sealed class WorkoutPlanOption
{
    private readonly List<MuscleGroupId> _muscleGroupIds;

    internal WorkoutPlanOption(
        WorkoutPlanOptionId id,
        int position,
        IReadOnlyList<MuscleGroupId> muscleGroupIds)
    {
        Id = id;
        Position = position;
        _muscleGroupIds = [.. Ensure(muscleGroupIds)];
    }

    public WorkoutPlanOptionId Id { get; }

    /// <summary>One-based order inside the plan, kept contiguous by the aggregate.</summary>
    public int Position { get; private set; }

    public IReadOnlyList<MuscleGroupId> MuscleGroupIds => _muscleGroupIds;

    internal void ChangeMuscleGroups(IReadOnlyList<MuscleGroupId> muscleGroupIds)
    {
        var validated = Ensure(muscleGroupIds);

        _muscleGroupIds.Clear();
        _muscleGroupIds.AddRange(validated);
    }

    internal void MoveTo(int position) => Position = position;

    private static IReadOnlyList<MuscleGroupId> Ensure(IReadOnlyList<MuscleGroupId> muscleGroupIds) =>
        muscleGroupIds.Count == 0
            ? throw new DomainException(
                "workout_plan.option.muscle_groups.empty",
                "A plan option must list at least one muscle group.")
            : muscleGroupIds;
}

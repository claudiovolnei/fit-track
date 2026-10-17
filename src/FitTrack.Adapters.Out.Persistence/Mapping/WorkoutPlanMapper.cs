using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>
/// The rows the aggregate added that the caller must insert explicitly. EF marks an entity discovered
/// in a navigation as <c>Modified</c> when its key is already set, so a new option (its id comes from
/// the application) would never be inserted — and the links would then break on a foreign key.
/// </summary>
internal sealed record WorkoutPlanChanges(
    IReadOnlyList<WorkoutPlanOptionRecord> Options,
    IReadOnlyList<WorkoutPlanOptionMuscleGroupRecord> Links)
{
    public static WorkoutPlanChanges None { get; } = new([], []);
}

/// <summary>Translates between the <see cref="WorkoutPlan"/> aggregate and its persistence records.</summary>
internal static class WorkoutPlanMapper
{
    public static WorkoutPlan ToDomain(WorkoutPlanRecord record)
    {
        var plan = WorkoutPlan.Start(
            new WorkoutPlanId(record.Id),
            new UserId(record.UserId),
            new UserId(record.TrainerId));

        foreach (var option in record.Options.OrderBy(option => option.Position))
        {
            plan.AddOption(
                new WorkoutPlanOptionId(option.Id),
                [.. option.MuscleGroups
                    .OrderBy(link => link.MuscleGroupId)
                    .Select(link => new MuscleGroupId(link.MuscleGroupId))]);
        }

        return plan;
    }

    /// <summary>The whole graph, for a plan that does not exist yet.</summary>
    public static WorkoutPlanRecord ToRecord(WorkoutPlan plan) => new()
    {
        Id = plan.Id.Value,
        UserId = plan.UserId.Value,
        TrainerId = plan.TrainerId.Value,
        Options = [.. plan.Options.Select(option => ToOptionRecord(plan.Id, option))],
    };

    /// <summary>
    /// Brings the stored rows in line with the aggregate. Options and links that are already stored are
    /// updated in place — so renumbering cannot collide with a row that is about to be deleted — and
    /// whatever is missing comes back for the caller to insert.
    /// </summary>
    public static WorkoutPlanChanges CopyInto(WorkoutPlan plan, WorkoutPlanRecord record)
    {
        var stored = record.Options.ToDictionary(option => option.Id);
        var kept = new HashSet<Guid>();
        var addedOptions = new List<WorkoutPlanOptionRecord>();
        var addedLinks = new List<WorkoutPlanOptionMuscleGroupRecord>();

        foreach (var option in plan.Options)
        {
            kept.Add(option.Id.Value);

            if (stored.TryGetValue(option.Id.Value, out var optionRecord))
            {
                optionRecord.Position = option.Position;
                addedLinks.AddRange(ReconcileMuscleGroups(option, optionRecord));
            }
            else
            {
                var newOption = ToOptionRecord(plan.Id, option);
                addedOptions.Add(newOption);
                addedLinks.AddRange(newOption.MuscleGroups);
            }
        }

        foreach (var removed in record.Options.Where(option => !kept.Contains(option.Id)).ToList())
        {
            record.Options.Remove(removed);
        }

        return addedOptions.Count == 0 && addedLinks.Count == 0
            ? WorkoutPlanChanges.None
            : new WorkoutPlanChanges(addedOptions, addedLinks);
    }

    private static WorkoutPlanOptionRecord ToOptionRecord(WorkoutPlanId planId, WorkoutPlanOption option) => new()
    {
        Id = option.Id.Value,
        WorkoutPlanId = planId.Value,
        Position = option.Position,
        MuscleGroups =
        [
            .. option.MuscleGroupIds.Select(id => ToLinkRecord(option.Id, id)),
        ],
    };

    private static WorkoutPlanOptionMuscleGroupRecord ToLinkRecord(
        WorkoutPlanOptionId optionId,
        MuscleGroupId muscleGroupId) => new()
    {
        WorkoutPlanOptionId = optionId.Value,
        MuscleGroupId = muscleGroupId.Value,
    };

    private static IReadOnlyList<WorkoutPlanOptionMuscleGroupRecord> ReconcileMuscleGroups(
        WorkoutPlanOption option,
        WorkoutPlanOptionRecord record)
    {
        var wanted = option.MuscleGroupIds.Select(id => id.Value).ToHashSet();

        foreach (var stale in record.MuscleGroups.Where(link => !wanted.Contains(link.MuscleGroupId)).ToList())
        {
            record.MuscleGroups.Remove(stale);
        }

        var stored = record.MuscleGroups.Select(link => link.MuscleGroupId).ToHashSet();

        return [.. wanted.Where(id => !stored.Contains(id)).Select(id => ToLinkRecord(option.Id, new MuscleGroupId(id)))];
    }
}

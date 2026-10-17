namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>Persistence model for a training plan. Deliberately separate from the aggregate.</summary>
public sealed class WorkoutPlanRecord
{
    public Guid Id { get; set; }

    /// <summary>The athlete the plan belongs to. Unique: one plan per athlete.</summary>
    public Guid UserId { get; set; }

    public Guid TrainerId { get; set; }

    public List<WorkoutPlanOptionRecord> Options { get; set; } = [];
}

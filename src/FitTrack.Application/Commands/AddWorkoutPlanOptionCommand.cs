namespace FitTrack.Application.Commands;

/// <summary>Intention: add an option (muscle groups to train) to a plan.</summary>
public sealed record AddWorkoutPlanOptionCommand(Guid PlanId, IReadOnlyList<Guid> MuscleGroupIds);

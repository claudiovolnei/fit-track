namespace FitTrack.Application.Commands;

/// <summary>Intention: change which muscle groups one plan option covers.</summary>
public sealed record ChangeWorkoutPlanOptionCommand(
    Guid PlanId,
    Guid OptionId,
    IReadOnlyList<Guid> MuscleGroupIds);

namespace FitTrack.Application.Commands;

/// <summary>Intention: drop one option from a plan.</summary>
public sealed record RemoveWorkoutPlanOptionCommand(Guid PlanId, Guid OptionId);

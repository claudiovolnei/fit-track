namespace FitTrack.Application.Commands;

/// <summary>Intention: discard a workout the athlete started by mistake, before logging anything.</summary>
public sealed record CancelWorkoutCommand(Guid WorkoutSessionId);

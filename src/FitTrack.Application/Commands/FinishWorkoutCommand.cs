namespace FitTrack.Application.Commands;

/// <summary>Intention: stop the timer and close the workout the athlete was doing.</summary>
public sealed record FinishWorkoutCommand(Guid WorkoutSessionId);

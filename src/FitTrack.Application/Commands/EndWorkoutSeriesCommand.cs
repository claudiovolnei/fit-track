namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: end the cycle the athlete is in, leaving the athlete without a series so the trainer
/// can create the next one when the time comes.
/// </summary>
public sealed record EndWorkoutSeriesCommand(Guid AthleteId);

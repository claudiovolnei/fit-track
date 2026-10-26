namespace FitTrack.Application.Commands;

public sealed record DeleteAthleteWorkoutCommand(Guid AthleteId, Guid SessionId);

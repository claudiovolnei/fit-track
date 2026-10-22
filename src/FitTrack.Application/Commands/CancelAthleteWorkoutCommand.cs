namespace FitTrack.Application.Commands;

/// <summary>Intention: cancel a running workout of one athlete, freeing their day.</summary>
public sealed record CancelAthleteWorkoutCommand(Guid AthleteId, Guid SessionId);
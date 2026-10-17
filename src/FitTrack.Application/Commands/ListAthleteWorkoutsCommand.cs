namespace FitTrack.Application.Commands;

/// <summary>Intention: read the workout history of one athlete, to follow their training.</summary>
public sealed record ListAthleteWorkoutsCommand(Guid UserId);

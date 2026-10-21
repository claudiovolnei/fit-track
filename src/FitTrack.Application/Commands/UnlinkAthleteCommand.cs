namespace FitTrack.Application.Commands;

/// <summary>Intention: undo the roster link of one athlete, whoever holds it.</summary>
public sealed record UnlinkAthleteCommand(Guid AthleteId);

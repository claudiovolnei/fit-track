namespace FitTrack.Domain.Identity;

/// <summary>Identifies the roster link between one athlete and one personal trainer.</summary>
public readonly record struct TrainerAthleteAssignmentId(Guid Value);

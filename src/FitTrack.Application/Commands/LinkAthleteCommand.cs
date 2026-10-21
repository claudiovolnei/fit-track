namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: link one athlete to one personal trainer. The personal trainer links to themselves —
/// <see cref="PersonalTrainerId"/> is empty — and the administrator names which trainer takes the row.
/// </summary>
public sealed record LinkAthleteCommand(Guid AthleteId, Guid? PersonalTrainerId = null);

namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: prescribe a series that already exists to one or more athletes, the way
/// <see cref="CreateWorkoutSeriesCommand"/> does for the one being born. The series stays as it is —
/// there is nothing to copy: what changes is who follows it.
/// </summary>
public sealed record AssignWorkoutSeriesCommand(
    Guid SeriesId,
    Guid? AthleteId = null,
    IReadOnlyList<Guid>? AthleteIds = null);

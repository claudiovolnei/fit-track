namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: create a reusable workout series authored by the personal trainer. When
/// <paramref name="CopyPreviousWorkouts"/> is set, the new series starts with the sheet of the
/// cycle the athlete is coming from, so the next cycle moves the loads instead of the whole plan.
/// </summary>
public sealed record CreateWorkoutSeriesCommand(
    string Name,
    Guid? AthleteId = null,
    bool CopyPreviousWorkouts = false);

namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: create a reusable workout series authored by the personal trainer. The series may be
/// prescribed to one athlete or to several of their roster at once (each destination keeps its own
/// <see cref="FitTrack.Domain.WorkoutPlanning.WorkoutSeriesAssignment"/>). When
/// <paramref name="CopyPreviousWorkouts"/> is set, the new series starts with the sheet of the cycle
/// the athletes are coming from: the sheet of the first destination that holds one, so a single
/// request moves the loads instead of the whole plan once for everyone.
/// </summary>
public sealed record CreateWorkoutSeriesCommand(
    string Name,
    Guid? AthleteId = null,
    bool CopyPreviousWorkouts = false,
    IReadOnlyList<Guid>? AthleteIds = null);

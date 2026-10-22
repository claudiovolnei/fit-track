namespace FitTrack.Application.Dtos;

/// <summary>
/// The series an athlete is following, with the state of their term: when it ends, how many days are
/// left and whether it already expired. <see cref="DaysRemaining"/> is zero once expired.
/// <see cref="IsSelfAuthored"/> says the athlete built the plan themselves, with the release the
/// administrator gave them — then <see cref="TrainerName"/> is their own name, and the screen says
/// "montado por você" instead of naming a personal trainer.
/// </summary>
public sealed record MyWorkoutSeriesDto(
    WorkoutSeriesDto Series,
    string TrainerName,
    DateTimeOffset ExpiresAt,
    int DaysRemaining,
    bool IsExpired)
{
    /// <summary>True when the athlete is following a plan they wrote themselves.</summary>
    public bool IsSelfAuthored { get; init; }
}

namespace FitTrack.Application.Dtos;

/// <summary>
/// The series an athlete is following, with the state of their term: when it ends, how many days are
/// left and whether it already expired. <see cref="DaysRemaining"/> is zero once expired.
/// </summary>
public sealed record MyWorkoutSeriesDto(
    WorkoutSeriesDto Series,
    string TrainerName,
    DateTimeOffset ExpiresAt,
    int DaysRemaining,
    bool IsExpired);

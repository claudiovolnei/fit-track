namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of an athlete the trainer may assign a series to. The series fields are null while the
/// athlete has nothing assigned; <see cref="DaysRemaining"/> is zero once the term expired.
/// </summary>
public sealed record AthleteDto(
    Guid UserId,
    string Email,
    Guid? WorkoutSeriesId,
    string? WorkoutSeriesName,
    int DaysRemaining);

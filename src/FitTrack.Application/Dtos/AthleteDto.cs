namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of an athlete the roster grants access to. The series fields are null while the
/// athlete has nothing assigned; <see cref="DaysRemaining"/> is zero once the term expired. The
/// trainer's e-mail comes filled only for the administrator's read, which sees every roster.
/// </summary>
public sealed record AthleteDto(
    Guid UserId,
    string Email,
    Guid? WorkoutSeriesId,
    string? WorkoutSeriesName,
    int DaysRemaining,
    string? PersonalTrainerEmail = null);

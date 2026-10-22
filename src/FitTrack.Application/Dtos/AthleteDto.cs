namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of an athlete the roster grants access to. The series fields are null while the
/// athlete has nothing assigned; <see cref="DaysRemaining"/> is zero once the term expired. The
/// trainer's e-mail comes filled only for the administrator's read, which sees every roster.
/// <see cref="EndedSeriesName"/> names the series of the cycle the trainer closed early, so the
/// roster shows what the athlete was doing and the next cycle can start from the same sheet.
/// </summary>
public sealed record AthleteDto(
    Guid UserId,
    string Email,
    Guid? WorkoutSeriesId,
    string? WorkoutSeriesName,
    int DaysRemaining,
    string? PersonalTrainerEmail = null,
    string? EndedSeriesName = null);

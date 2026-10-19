namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of one specific workout of a series. <see cref="Label"/> is the letter the athlete reads
/// ("A".."E"), derived from <see cref="Position"/>. <see cref="Today"/> stays null for the trainer's
/// screens, which do not run a timer.
/// </summary>
public sealed record WorkoutSeriesWorkoutDto(
    Guid Id,
    int Position,
    string Label,
    string Name,
    IReadOnlyList<PrescribedExerciseDto> Exercises,
    TodayWorkoutDto? Today);

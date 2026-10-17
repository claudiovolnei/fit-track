namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of one plan option: what to train (muscle groups) and whether it was already trained
/// today. <see cref="Today"/> stays null for the trainer's screens, which do not run a timer.
/// </summary>
public sealed record WorkoutPlanOptionDto(
    Guid Id,
    int Position,
    IReadOnlyList<MuscleGroupDto> MuscleGroups,
    TodayWorkoutDto? Today);

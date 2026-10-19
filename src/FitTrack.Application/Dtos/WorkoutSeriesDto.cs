namespace FitTrack.Application.Dtos;

/// <summary>Read model of a reusable workout series a trainer authored.</summary>
public sealed record WorkoutSeriesDto(
    Guid Id,
    string Name,
    Guid TrainerId,
    IReadOnlyList<WorkoutSeriesWorkoutDto> Workouts);

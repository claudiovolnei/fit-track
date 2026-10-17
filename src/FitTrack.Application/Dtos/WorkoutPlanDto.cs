namespace FitTrack.Application.Dtos;

/// <summary>Read model of a training plan a trainer assigned to an athlete.</summary>
public sealed record WorkoutPlanDto(
    Guid Id,
    Guid UserId,
    Guid TrainerId,
    IReadOnlyList<WorkoutPlanOptionDto> Options);

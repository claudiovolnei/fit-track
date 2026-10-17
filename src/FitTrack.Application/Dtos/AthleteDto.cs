namespace FitTrack.Application.Dtos;

/// <summary>Read model of an athlete the trainer may assign a plan to.</summary>
public sealed record AthleteDto(Guid UserId, string Email, bool HasPlan);

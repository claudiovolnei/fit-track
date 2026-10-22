namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of one athlete following a series: who they are, whether they are in it and how long
/// the term runs. <see cref="Name"/> comes straight off the account; a caller with nothing to show
/// falls back to the e-mail.
/// </summary>
public sealed record WorkoutSeriesFollowerDto(
    Guid UserId,
    string Email,
    string? Name,
    /// <summary>False when the trainer closed the cycle and the athlete follows nothing.</summary>
    bool IsOpen,
    DateTimeOffset ExpiresAt,
    /// <summary>Whole days left before the term ends, or zero once expired.</summary>
    int DaysRemaining);

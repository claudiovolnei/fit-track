namespace FitTrack.Integration.Contracts;

/// <summary>One athlete following a series, with the state of their term.</summary>
public sealed record SeriesFollowerResponse(
    Guid UserId,
    string Email,
    string? Name,
    bool IsOpen,
    DateTimeOffset ExpiresAt,
    int DaysRemaining);

/// <summary>Wire contract for assigning a series that already exists to one or more athletes.</summary>
public sealed record AssignSeriesRequest(Guid? AthleteId = null, IReadOnlyList<Guid>? AthleteIds = null);

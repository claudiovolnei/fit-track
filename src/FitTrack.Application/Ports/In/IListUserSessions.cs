using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the current user's workout sessions.</summary>
public interface IListUserSessions
{
    Task<IReadOnlyList<WorkoutSessionDto>> ExecuteAsync(CancellationToken cancellationToken);

    Task<WorkoutHistoryDto> ExecuteAsync(ListQuery query, CancellationToken cancellationToken);
}

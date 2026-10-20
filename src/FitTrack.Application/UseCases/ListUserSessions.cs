using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the workout sessions owned by the authenticated user.</summary>
public sealed class ListUserSessions : IListUserSessions
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryUserSessions _query;

    public ListUserSessions(ICurrentUser currentUser, IQueryUserSessions query)
    {
        _currentUser = currentUser;
        _query = query;
    }

    public Task<IReadOnlyList<WorkoutSessionDto>> ExecuteAsync(CancellationToken cancellationToken) =>
        _query.ListByUserAsync(_currentUser.Id, cancellationToken);

    public Task<WorkoutHistoryDto> ExecuteAsync(
        ListQuery query,
        CancellationToken cancellationToken) =>
        _query.ListByUserAsync(_currentUser.Id, query, cancellationToken);
}

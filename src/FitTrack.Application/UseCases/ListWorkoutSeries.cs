using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Lists the series the caller authored: the personal trainer their own, the athlete released by the
/// administrator their own plan.
/// </summary>
public sealed class ListWorkoutSeries : IListWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;
    private readonly IAccountPermissions _permissions;

    public ListWorkoutSeries(
        ICurrentUser currentUser,
        IQueryWorkoutSeries query,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _query = query;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<WorkoutSeriesDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "see the workout series they authored",
            cancellationToken);

        return await _query.ListByTrainerAsync(_currentUser.Id, cancellationToken);
    }

    public async Task<PagedResult<WorkoutSeriesDto>> ExecuteAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "see the workout series they authored",
            cancellationToken);

        return await _query.ListByTrainerAsync(_currentUser.Id, query, cancellationToken);
    }
}

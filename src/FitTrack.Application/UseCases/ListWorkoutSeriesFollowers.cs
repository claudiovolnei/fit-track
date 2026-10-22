using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Reads the athletes a series serves. A series authored by someone else answers as if it did not
/// exist, the same way reading the series itself does — the read never reveals whose athletes the
/// other author is training.
/// </summary>
public sealed class ListWorkoutSeriesFollowers : IListWorkoutSeriesFollowers
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;
    private readonly IAccountPermissions _permissions;
    private readonly IClock _clock;

    public ListWorkoutSeriesFollowers(
        ICurrentUser currentUser,
        IQueryWorkoutSeries query,
        IAccountPermissions permissions,
        IClock clock)
    {
        _currentUser = currentUser;
        _query = query;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<IReadOnlyList<WorkoutSeriesFollowerDto>> ExecuteAsync(
        Guid seriesId,
        CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "see the athletes following the series they authored",
            cancellationToken);

        var series = await _query.GetByIdAsync(new WorkoutSeriesId(seriesId), cancellationToken);

        if (series is null || series.TrainerId != _currentUser.Id.Value)
        {
            throw new NotFoundException($"Workout series '{seriesId}' was not found.");
        }

        return await _query.ListFollowersAsync(
            new WorkoutSeriesId(seriesId),
            _clock.UtcNow,
            cancellationToken);
    }
}

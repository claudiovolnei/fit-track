using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>Reads the roster screens: the personal trainer sees their own athletes, the admin all of them.</summary>
public sealed class ListAthletes : IListAthletes
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryAthletes _athletes;
    private readonly IClock _clock;

    public ListAthletes(ICurrentUser currentUser, IQueryAthletes athletes, IClock clock)
    {
        _currentUser = currentUser;
        _athletes = athletes;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AthleteDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("list the athletes");

        if (_currentUser.IsAdmin)
        {
            var whole = await _athletes.ListAsync(_clock.UtcNow, ListQuery.All, cancellationToken);
            return whole.Items;
        }

        return await _athletes.ListAsync(_clock.UtcNow, _currentUser.Id, cancellationToken);
    }

    public Task<PagedResult<AthleteDto>> ExecuteAsync(
        ListQuery query,
        CancellationToken cancellationToken) =>
        ExecuteAsync(query, unlinkedOnly: false, cancellationToken);

    public async Task<PagedResult<AthleteDto>> ExecuteAsync(
        ListQuery query,
        bool unlinkedOnly,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("list the athletes");

        if (unlinkedOnly)
        {
            return await _athletes.ListUnlinkedAsync(_clock.UtcNow, query, cancellationToken);
        }

        if (_currentUser.IsAdmin)
        {
            return await _athletes.ListAsync(_clock.UtcNow, query, cancellationToken);
        }

        return await _athletes.ListAsync(_clock.UtcNow, query, _currentUser.Id, cancellationToken);
    }
}

using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the athletes available to receive a series. Restricted to personal trainers.</summary>
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

    public Task<IReadOnlyList<AthleteDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("list the athletes");

        return _athletes.ListAsync(_clock.UtcNow, cancellationToken);
    }
}

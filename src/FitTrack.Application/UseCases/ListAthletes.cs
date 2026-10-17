using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the athletes available to receive a plan. Restricted to personal trainers.</summary>
public sealed class ListAthletes : IListAthletes
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryAthletes _athletes;

    public ListAthletes(ICurrentUser currentUser, IQueryAthletes athletes)
    {
        _currentUser = currentUser;
        _athletes = athletes;
    }

    public Task<IReadOnlyList<AthleteDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("list the athletes");

        return _athletes.ListAsync(cancellationToken);
    }
}

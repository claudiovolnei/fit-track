using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the series the personal trainer authored.</summary>
public sealed class ListWorkoutSeries : IListWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;

    public ListWorkoutSeries(ICurrentUser currentUser, IQueryWorkoutSeries query)
    {
        _currentUser = currentUser;
        _query = query;
    }

    public Task<IReadOnlyList<WorkoutSeriesDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("see the workout series they authored");

        return _query.ListByTrainerAsync(_currentUser.Id, cancellationToken);
    }
}

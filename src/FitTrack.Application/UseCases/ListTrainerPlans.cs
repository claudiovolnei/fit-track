using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>Lists the plans the personal trainer authored.</summary>
public sealed class ListTrainerPlans : IListTrainerPlans
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutPlans _query;

    public ListTrainerPlans(ICurrentUser currentUser, IQueryWorkoutPlans query)
    {
        _currentUser = currentUser;
        _query = query;
    }

    public Task<IReadOnlyList<WorkoutPlanDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("see the workout plans they authored");

        return _query.ListByTrainerAsync(_currentUser.Id, cancellationToken);
    }
}

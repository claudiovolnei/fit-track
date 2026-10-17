using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Shows the athlete their own plan with the state of today: for each option, whether the workout was
/// already started, whether it is running and how long it has been going.
/// </summary>
public sealed class GetMyWorkoutPlan : IGetMyWorkoutPlan
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutPlans _query;
    private readonly IClock _clock;

    public GetMyWorkoutPlan(ICurrentUser currentUser, IQueryWorkoutPlans query, IClock clock)
    {
        _currentUser = currentUser;
        _query = query;
        _clock = clock;
    }

    public Task<WorkoutPlanDto?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        return _query.GetByAthleteAsync(
            _currentUser.Id,
            DateOnly.FromDateTime(now.UtcDateTime),
            now,
            cancellationToken);
    }
}

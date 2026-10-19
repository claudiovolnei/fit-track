using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Drops a workout from a series and renumbers the labels that follow, so the sheet stays A..E with
/// no hole. Workouts already started from it stay in the athlete's history: the session references
/// the workout id and the removal does not cascade into the execution context.
/// Restricted to the trainer who authored the series.
/// </summary>
public sealed class RemoveSeriesWorkout : IRemoveSeriesWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;

    public RemoveSeriesWorkout(ICurrentUser currentUser, IWorkoutSeriesRepository series)
    {
        _currentUser = currentUser;
        _series = series;
    }

    public async Task ExecuteAsync(RemoveSeriesWorkoutCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("build a workout series");

        var series = await WorkoutSeriesAccess.RequireOwnedAsync(
            _series,
            command.SeriesId,
            _currentUser.Id,
            cancellationToken);

        series.RemoveWorkout(new WorkoutSeriesWorkoutId(command.WorkoutId));

        await _series.SaveAsync(series, cancellationToken);
    }
}

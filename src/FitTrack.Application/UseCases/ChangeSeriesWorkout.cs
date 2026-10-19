using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Replaces the name and the prescription of one workout. Editing a workout never touches a session
/// already started from it: the session keeps the workout id, not a copy of the prescription.
/// Restricted to the trainer who authored the series.
/// </summary>
public sealed class ChangeSeriesWorkout : IChangeSeriesWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IExerciseRepository _exercises;
    private readonly IQueryWorkoutSeries _query;

    public ChangeSeriesWorkout(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IExerciseRepository exercises,
        IQueryWorkoutSeries query)
    {
        _currentUser = currentUser;
        _series = series;
        _exercises = exercises;
        _query = query;
    }

    public async Task<WorkoutSeriesWorkoutDto> ExecuteAsync(
        ChangeSeriesWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("build a workout series");

        var series = await WorkoutSeriesAccess.RequireOwnedAsync(
            _series,
            command.SeriesId,
            _currentUser.Id,
            cancellationToken);

        var exercises = command.Exercises.ToPrescribed();
        var workoutId = new WorkoutSeriesWorkoutId(command.WorkoutId);

        await ExerciseLookup.RequireAllAsync(_exercises, exercises, cancellationToken);

        series.ChangeWorkout(workoutId, new WorkoutName(command.Name), exercises);

        await _series.SaveAsync(series, cancellationToken);

        var dto = await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);

        return dto.Workouts.Single(candidate => candidate.Id == workoutId.Value);
    }
}

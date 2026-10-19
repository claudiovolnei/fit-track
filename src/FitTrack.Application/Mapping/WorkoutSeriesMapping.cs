using FitTrack.Application.Dtos;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Mapping;

/// <summary>
/// The catalog names the series read model needs, resolved once. A workout prescribes exercises, and
/// each exercise carries its own muscle group, so the two names travel together.
/// </summary>
internal sealed record ExerciseLabel(string Name, string MuscleGroupName);

/// <summary>
/// Translates a <see cref="WorkoutSeries"/> into its read model. The aggregate only holds exercise
/// ids, so the caller resolves the names once and passes the lookup in; the per-workout today state
/// comes as a lookup too, so a workout with no session today stays null.
/// </summary>
internal static class WorkoutSeriesMapping
{
    public static readonly IReadOnlyDictionary<Guid, ExerciseLabel> NoExercises =
        new Dictionary<Guid, ExerciseLabel>();

    public static readonly IReadOnlyDictionary<Guid, TodayWorkoutDto> NoToday =
        new Dictionary<Guid, TodayWorkoutDto>();

    public static WorkoutSeriesDto ToDto(
        this WorkoutSeries series,
        IReadOnlyDictionary<Guid, ExerciseLabel> exercises,
        IReadOnlyDictionary<Guid, TodayWorkoutDto> todayByWorkout) =>
        new(
            series.Id.Value,
            series.Name.Value,
            series.TrainerId.Value,
            series.AthleteId?.Value,
            [.. series.Workouts.Select(workout =>
                workout.ToDto(exercises, todayByWorkout.GetValueOrDefault(workout.Id.Value)))]);

    public static WorkoutSeriesWorkoutDto ToDto(
        this WorkoutSeriesWorkout workout,
        IReadOnlyDictionary<Guid, ExerciseLabel> exercises,
        TodayWorkoutDto? today) =>
        new(
            workout.Id.Value,
            workout.Position,
            workout.Label,
            workout.Name.Value,
            [.. workout.Exercises.Select(exercise =>
            {
                var label = exercises[exercise.ExerciseId.Value];

                return new PrescribedExerciseDto(
                    exercise.ExerciseId.Value,
                    label.Name,
                    label.MuscleGroupName,
                    exercise.Sets,
                    exercise.Repetitions,
                    exercise.LoadKilograms);
            })],
            today);
}

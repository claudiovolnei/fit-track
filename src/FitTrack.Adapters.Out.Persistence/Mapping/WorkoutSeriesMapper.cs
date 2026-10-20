using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>
/// The rows the aggregate added that the caller must insert explicitly. EF marks an entity discovered
/// in a navigation as <c>Modified</c> when its key is already set, so a new workout (its id comes from
/// the application) would never be inserted — and its prescription rows would then break on a foreign
/// key.
/// </summary>
internal sealed record WorkoutSeriesChanges(
    IReadOnlyList<WorkoutSeriesWorkoutRecord> Workouts,
    IReadOnlyList<WorkoutSeriesWorkoutExerciseRecord> Exercises)
{
    public static WorkoutSeriesChanges None { get; } = new([], []);
}

/// <summary>Translates between the <see cref="WorkoutSeries"/> aggregate and its persistence records.</summary>
internal static class WorkoutSeriesMapper
{
    public static WorkoutSeries ToDomain(WorkoutSeriesRecord record)
    {
        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(record.Id),
            new WorkoutSeriesName(record.Name),
            new UserId(record.TrainerId),
            record.AthleteId is { } athlete ? new UserId(athlete) : null);

        // The stored order is what rebuilds the positions: AddWorkout numbers 1..n as they enter, so
        // the aggregate keeps the sheet contiguous without the column being the source of the rule.
        foreach (var workout in record.Workouts.OrderBy(workout => workout.Position))
        {
            series.AddWorkout(
                new WorkoutSeriesWorkoutId(workout.Id),
                new WorkoutName(workout.Name),
                [.. workout.Exercises
                    .OrderBy(exercise => exercise.Position)
                    .Select(exercise => new PrescribedExercise(
                        new ExerciseId(exercise.ExerciseId),
                        exercise.Sets,
                        exercise.Repetitions,
                        exercise.LoadKilograms))]);
        }

        return series;
    }

    /// <summary>The whole graph, for a series that does not exist yet.</summary>
    public static WorkoutSeriesRecord ToRecord(WorkoutSeries series) => new()
    {
        Id = series.Id.Value,
        Name = series.Name.Value,
        TrainerId = series.TrainerId.Value,
        AthleteId = series.AthleteId?.Value,
        Workouts = [.. series.Workouts.Select(workout => ToWorkoutRecord(series.Id, workout))],
    };

    /// <summary>
    /// Brings the stored rows in line with the aggregate. Workouts and prescription rows that are
    /// already stored are updated in place — so renumbering cannot collide with a row that is about to
    /// be deleted — and whatever is missing comes back for the caller to insert.
    /// </summary>
    public static WorkoutSeriesChanges CopyInto(WorkoutSeries series, WorkoutSeriesRecord record)
    {
        record.Name = series.Name.Value;
        record.AthleteId = series.AthleteId?.Value;

        var stored = record.Workouts.ToDictionary(workout => workout.Id);
        var kept = new HashSet<Guid>();
        var addedWorkouts = new List<WorkoutSeriesWorkoutRecord>();
        var addedExercises = new List<WorkoutSeriesWorkoutExerciseRecord>();

        foreach (var workout in series.Workouts)
        {
            kept.Add(workout.Id.Value);

            if (stored.TryGetValue(workout.Id.Value, out var workoutRecord))
            {
                workoutRecord.Position = workout.Position;
                workoutRecord.Name = workout.Name.Value;
                addedExercises.AddRange(ReconcileExercises(workout, workoutRecord));
            }
            else
            {
                var newWorkout = ToWorkoutRecord(series.Id, workout);
                addedWorkouts.Add(newWorkout);
                addedExercises.AddRange(newWorkout.Exercises);
            }
        }

        foreach (var removed in record.Workouts.Where(workout => !kept.Contains(workout.Id)).ToList())
        {
            record.Workouts.Remove(removed);
        }

        return addedWorkouts.Count == 0 && addedExercises.Count == 0
            ? WorkoutSeriesChanges.None
            : new WorkoutSeriesChanges(addedWorkouts, addedExercises);
    }

    private static WorkoutSeriesWorkoutRecord ToWorkoutRecord(WorkoutSeriesId seriesId, WorkoutSeriesWorkout workout) => new()
    {
        Id = workout.Id.Value,
        WorkoutSeriesId = seriesId.Value,
        Position = workout.Position,
        Name = workout.Name.Value,
        Exercises =
        [
            .. workout.Exercises.Select((exercise, position) => ToExerciseRecord(workout.Id, exercise, position)),
        ],
    };

    private static WorkoutSeriesWorkoutExerciseRecord ToExerciseRecord(
        WorkoutSeriesWorkoutId workoutId,
        PrescribedExercise exercise,
        int position) => new()
    {
        WorkoutSeriesWorkoutId = workoutId.Value,
        ExerciseId = exercise.ExerciseId.Value,
        Position = position,
        Sets = exercise.Sets,
        Repetitions = exercise.Repetitions,
        LoadKilograms = exercise.LoadKilograms,
    };

    /// <summary>
    /// Brings the stored prescription in line with the workout. The pair (workout, exercise) is the
    /// key, so an edit is an update in place, not a delete plus an insert; only the exercises that
    /// entered the workout come back for the caller to insert.
    /// </summary>
    private static IReadOnlyList<WorkoutSeriesWorkoutExerciseRecord> ReconcileExercises(
        WorkoutSeriesWorkout workout,
        WorkoutSeriesWorkoutRecord record)
    {
        var wanted = workout.Exercises.Select(exercise => exercise.ExerciseId.Value).ToHashSet();

        foreach (var stale in record.Exercises.Where(link => !wanted.Contains(link.ExerciseId)).ToList())
        {
            record.Exercises.Remove(stale);
        }

        var stored = record.Exercises.ToDictionary(link => link.ExerciseId);
        var added = new List<WorkoutSeriesWorkoutExerciseRecord>();

        for (var position = 0; position < workout.Exercises.Count; position++)
        {
            var exercise = workout.Exercises[position];

            if (stored.TryGetValue(exercise.ExerciseId.Value, out var link))
            {
                link.Position = position;
                link.Sets = exercise.Sets;
                link.Repetitions = exercise.Repetitions;
                link.LoadKilograms = exercise.LoadKilograms;
            }
            else
            {
                added.Add(ToExerciseRecord(workout.Id, exercise, position));
            }
        }

        return added;
    }
}

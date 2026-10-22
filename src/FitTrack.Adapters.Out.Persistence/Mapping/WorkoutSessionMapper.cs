using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>Translates between the <see cref="WorkoutSession"/> aggregate and its persistence record.</summary>
internal static class WorkoutSessionMapper
{
    public static WorkoutSession ToDomain(WorkoutSessionRecord record)
    {
        var session = record.WorkoutSeriesWorkoutId is { } workoutId
            ? WorkoutSession.StartFromPlannedWorkout(
                new WorkoutSessionId(record.Id),
                new UserId(record.UserId),
                new WorkoutDate(record.WorkoutDate),
                record.StartedAt,
                new WorkoutSeriesWorkoutId(workoutId))
            : WorkoutSession.Start(
                new WorkoutSessionId(record.Id),
                new UserId(record.UserId),
                new WorkoutDate(record.WorkoutDate),
                record.StartedAt);

        // Sets first: the aggregate refuses a set once the workout is finished.
        foreach (var set in record.Sets.OrderBy(set => set.Position))
        {
            session.RegisterSet(
                new ExerciseId(set.ExerciseId),
                new Load(set.LoadKilograms),
                new Repetitions(set.Repetitions));
        }

        // The ticks the rows carry are the truth: a session rebuilt from the database has to show the
        // same exercises ticked, whatever the sets say.
        foreach (var completed in record.CompletedExercises)
        {
            session.MarkExerciseCompleted(new ExerciseId(completed.ExerciseId));
        }

        if (record.FinishedAt is { } finishedAt)
        {
            session.Finish(
                finishedAt,
                record.Intensity is { } intensity ? (WorkoutIntensity)intensity : null,
                record.Notes is { } notes ? new WorkoutNotes(notes) : null);
        }

        return session;
    }

    public static WorkoutSessionRecord ToRecord(WorkoutSession session) => new()
    {
        Id = session.Id.Value,
        UserId = session.UserId.Value,
        WorkoutDate = session.WorkoutDate.Value,
        WorkoutSeriesWorkoutId = session.PlannedWorkoutId?.Value,
        StartedAt = session.StartedAt,
        FinishedAt = session.FinishedAt,
        Intensity = session.Intensity is { } intensity ? (int)intensity : null,
        Notes = session.Notes?.Value,
        Sets = [.. ToSetRecords(session)],
        CompletedExercises = [.. ToCompletedExerciseRecords(session)],
    };

    public static void CopyInto(WorkoutSession session, WorkoutSessionRecord record)
    {
        record.FinishedAt = session.FinishedAt;
        record.Intensity = session.Intensity is { } intensity ? (int)intensity : null;
        record.Notes = session.Notes?.Value;

        record.Sets.Clear();
        record.Sets.AddRange(ToSetRecords(session));

        record.CompletedExercises.Clear();
        record.CompletedExercises.AddRange(ToCompletedExerciseRecords(session));
    }

    /// <summary>
    /// The aggregate keeps the sets in the order they were registered, so the enumeration
    /// index is the position and it survives the round trip.
    /// </summary>
    public static IReadOnlyList<PerformedSetRecord> ToSetRecords(WorkoutSession session) =>
        [.. session.PerformedSets.Select((set, position) => ToSetRecord(set, position))];

    private static PerformedSetRecord ToSetRecord(PerformedSet set, int position) => new()
    {
        Position = position,
        ExerciseId = set.ExerciseId.Value,
        LoadKilograms = set.Load.Kilograms,
        Repetitions = set.Repetitions.Value,
    };

    /// <summary>The ticks of the session, one row per exercise the athlete marked as done.</summary>
    private static IReadOnlyList<CompletedExerciseRecord> ToCompletedExerciseRecords(WorkoutSession session) =>
        [.. session.CompletedExercises.Select(exercise => new CompletedExerciseRecord
        {
            ExerciseId = exercise.Value,
        })];
}

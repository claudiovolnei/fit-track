using FitTrack.Application.Dtos;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Compares the load the athlete performed with the prescription, one exercise at a time. Each
/// exercise contributes a representative load — the average over the athlete's loaded sets against
/// the prescription's load per set, weighted by sets — and the delta is the sum of those load
/// differences in kilograms of load, never multiplied by sets or repetitions. It is a pure
/// read-side rule: no aggregate changes, no persistence, so both the SQL adapter and the tests can
/// share exactly the same arithmetic.
/// </summary>
public static class WorkoutVolumeCalculator
{
    public const string Matched = "matched";
    public const string Bodyweight = "bodyweight";
    public const string NotPerformed = "notPerformed";
    public const string NotPrescribed = "notPrescribed";

    /// <summary>
    /// The session's load comparison, or null when there is no prescription to compare (no workout
    /// reference or a workout that no longer exists).
    /// </summary>
    public static WorkoutVolumeDto? Calculate(
        IReadOnlyList<PerformedSetDto> performedSets,
        IReadOnlyList<PrescribedExerciseDto>? prescription,
        IReadOnlyDictionary<Guid, string> exerciseNames)
    {
        if (prescription is null)
        {
            return null;
        }

        var prescribedByExercise = prescription
            .GroupBy(exercise => exercise.ExerciseId)
            .ToDictionary(
                group => group.Key,
                group => new PrescribedRow(
                    group.First().ExerciseName,
                    PrescribedLoad(group)));

        var performedByExercise = performedSets
            .GroupBy(set => set.ExerciseId)
            .ToDictionary(group => group.Key, group => PerformedLoad(group));

        var prescribedOrder = prescription
            .Select(exercise => exercise.ExerciseId)
            .Distinct()
            .ToList();

        var performedOnly = performedByExercise.Keys
            .Where(exerciseId => !prescribedByExercise.ContainsKey(exerciseId))
            .OrderBy(exerciseId => Name(exerciseId, exerciseNames), StringComparer.OrdinalIgnoreCase)
            .ThenBy(exerciseId => exerciseId)
            .ToList();

        var exercises = new List<ExerciseVolumeDto>(prescribedOrder.Count + performedOnly.Count);

        foreach (var exerciseId in prescribedOrder)
        {
            var prescribed = prescribedByExercise[exerciseId];
            var performed = performedByExercise.GetValueOrDefault(exerciseId);

            var status = prescribed.LoadKilograms is null
                ? Bodyweight
                : performedByExercise.ContainsKey(exerciseId) ? Matched : NotPerformed;

            exercises.Add(Item(
                exerciseId,
                prescribed.ExerciseName,
                performed ?? 0m,
                prescribed.LoadKilograms ?? 0m,
                status));
        }

        foreach (var exerciseId in performedOnly)
        {
            exercises.Add(Item(
                exerciseId,
                Name(exerciseId, exerciseNames),
                performedByExercise[exerciseId] ?? 0m,
                0m,
                NotPrescribed));
        }

        // Only exercises the prescription challenges with a load enter the session's comparison:
        // the matched ones contribute the athlete's load, the skipped ones contribute zero on that
        // side and keep their prescribed load, so the delta is the sum of one load difference per
        // exercise. Bodyweight and not-prescribed rows keep their display value but no base.
        var comparable = exercises.Where(exercise => exercise.Status is Matched or NotPerformed);
        var totalPerformed = comparable.Sum(exercise =>
            exercise.Status == Matched ? exercise.PerformedKilograms : 0m);
        var totalPrescribed = comparable.Sum(exercise => exercise.PrescribedKilograms);
        var delta = totalPerformed - totalPrescribed;

        return new WorkoutVolumeDto(
            totalPerformed,
            totalPrescribed,
            delta,
            Percent(totalPrescribed, delta),
            exercises);
    }

    /// <summary>The comparison over a whole history, using the load comparison already calculated per session.</summary>
    public static WorkoutVolumeSummaryDto Summarize(IReadOnlyList<WorkoutSessionDto> sessions)
    {
        // Sessions without a comparable prescription stay out of the load comparison; the count of
        // those sessions is what keeps the ratio honest.
        var performed = sessions.Sum(session => session.Volume?.PerformedKilograms ?? 0m);
        var prescribed = sessions.Sum(session => session.Volume?.PrescribedKilograms ?? 0m);
        var delta = performed - prescribed;

        return new WorkoutVolumeSummaryDto(
            sessions.Count,
            sessions.Sum(session => session.Sets.Count),
            performed,
            prescribed,
            delta,
            Percent(prescribed, delta),
            sessions.Count(session => session.Volume is null));
    }

    private static ExerciseVolumeDto Item(
        Guid exerciseId,
        string name,
        decimal performed,
        decimal prescribed,
        string status)
    {
        var delta = performed - prescribed;

        return new ExerciseVolumeDto(
            exerciseId,
            name,
            performed,
            prescribed,
            delta,
            Percent(prescribed, delta),
            status);
    }

    /// <summary>The prescription's load per set: the average over the rows that carry load, weighted by sets.</summary>
    private static decimal? PrescribedLoad(IEnumerable<PrescribedExerciseDto> rows)
    {
        var loaded = rows.Where(row => row.LoadKilograms is not null).ToList();
        var sets = loaded.Sum(row => row.Sets);

        return sets > 0 ? loaded.Sum(row => row.Sets * row.LoadKilograms!.Value) / sets : null;
    }

    /// <summary>The athlete's representative load: the average over the sets that carried load — zero-load (bodyweight) sets have no load to raise and do not dilute it.</summary>
    private static decimal? PerformedLoad(IEnumerable<PerformedSetDto> sets)
    {
        var loaded = sets.Where(set => set.LoadKilograms > 0m).ToList();

        return loaded.Count > 0 ? loaded.Sum(set => set.LoadKilograms) / loaded.Count : null;
    }

    /// <summary>Percentage with two decimals; null when the prescription has no load to compare against.</summary>
    private static decimal? Percent(decimal prescribed, decimal delta) =>
        prescribed > 0m
            ? Math.Round(delta / prescribed * 100m, 2, MidpointRounding.AwayFromZero)
            : null;

    private static string Name(Guid exerciseId, IReadOnlyDictionary<Guid, string> exerciseNames) =>
        exerciseNames.TryGetValue(exerciseId, out var name) ? name : exerciseId.ToString();

    private sealed record PrescribedRow(string ExerciseName, decimal? LoadKilograms);
}

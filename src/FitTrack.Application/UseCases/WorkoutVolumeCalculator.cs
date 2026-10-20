using FitTrack.Application.Dtos;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Compares the sets an athlete performed with the current prescription of the workout. It is a pure
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
    /// The session volume, or null when there is no prescription to compare (no workout reference or
    /// a workout that no longer exists).
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

        var performedByExercise = performedSets
            .GroupBy(set => set.ExerciseId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(set => set.LoadKilograms * set.Repetitions));

        var prescribedByExercise = prescription
            .GroupBy(exercise => exercise.ExerciseId)
            .ToDictionary(
                group => group.Key,
                group => new PrescribedRow(
                    group.First().ExerciseName,
                    group.First().LoadKilograms,
                    group.Sum(exercise => exercise.Sets * exercise.Repetitions * (exercise.LoadKilograms ?? 0m))));

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
                performed,
                prescribed.Volume,
                status));
        }

        foreach (var exerciseId in performedOnly)
        {
            exercises.Add(Item(
                exerciseId,
                Name(exerciseId, exerciseNames),
                performedByExercise[exerciseId],
                0m,
                NotPrescribed));
        }

        var totalPerformed = exercises.Sum(exercise => exercise.PerformedKilograms);
        var totalPrescribed = exercises.Sum(exercise => exercise.PrescribedKilograms);
        var delta = totalPerformed - totalPrescribed;

        return new WorkoutVolumeDto(
            totalPerformed,
            totalPrescribed,
            delta,
            Percent(totalPrescribed, delta),
            exercises);
    }

    /// <summary>The comparison over a whole history, using the volume already calculated per session.</summary>
    public static WorkoutVolumeSummaryDto Summarize(IReadOnlyList<WorkoutSessionDto> sessions)
    {
        // The performed side is the work itself, so it includes legacy sessions with no prescription to
        // compare against; the count of those sessions is what keeps the ratio honest.
        var performed = sessions.Sum(session =>
            session.Sets.Sum(set => set.LoadKilograms * set.Repetitions));

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

    /// <summary>Percentage with two decimals; null when the prescription has no load to compare against.</summary>
    private static decimal? Percent(decimal prescribed, decimal delta) =>
        prescribed > 0m
            ? Math.Round(delta / prescribed * 100m, 2, MidpointRounding.AwayFromZero)
            : null;

    private static string Name(Guid exerciseId, IReadOnlyDictionary<Guid, string> exerciseNames) =>
        exerciseNames.TryGetValue(exerciseId, out var name) ? name : exerciseId.ToString();

    private sealed record PrescribedRow(string ExerciseName, decimal? LoadKilograms, decimal Volume);
}

using FitTrack.UI.Api;

namespace FitTrack.UI.Components;

/// <summary>
/// Presentation grouping for the workout-history card: consecutive sets performed the same
/// way are one "run", so the card shows "4 × 20 · 20 kg" instead of four identical rows.
/// Pure read over the wire responses, in list order — nothing here is domain knowledge.
/// </summary>
public static class SessionSetGroups
{
    /// <summary>
    /// One run of consecutive identical sets. <see cref="FirstSetIndex"/> and
    /// <see cref="SetIndexes"/> are zero-based positions in the session's <c>Sets</c> list,
    /// so two separate runs of the same exercise in one session stay distinguishable.
    /// </summary>
    public sealed record SetRun(
        Guid ExerciseId,
        decimal LoadKilograms,
        int Repetitions,
        int FirstSetIndex,
        IReadOnlyList<int> SetIndexes);

    /// <summary>Splits the sets into runs of neighbors equal in exercise, repetitions and load.</summary>
    public static IReadOnlyList<SetRun> Group(IReadOnlyList<PerformedSetResponse> sets)
    {
        if (sets.Count == 0)
        {
            return [];
        }

        var runs = new List<SetRun>();

        // The performed load is a non-nullable decimal on the wire (a bodyweight set is stored
        // as a value, not a null), so the comparison breaks a run on any change of the three fields.
        var indexes = new List<int> { 0 };

        for (var i = 1; i < sets.Count; i++)
        {
            var previous = sets[i - 1];
            var current = sets[i];

            if (current.ExerciseId != previous.ExerciseId ||
                current.Repetitions != previous.Repetitions ||
                current.LoadKilograms != previous.LoadKilograms)
            {
                runs.Add(new SetRun(
                    previous.ExerciseId,
                    previous.LoadKilograms,
                    previous.Repetitions,
                    indexes[0],
                    indexes));
                indexes = [];
            }

            indexes.Add(i);
        }

        var last = sets[^1];

        runs.Add(new SetRun(
            last.ExerciseId,
            last.LoadKilograms,
            last.Repetitions,
            indexes[0],
            indexes));

        return runs;
    }
}

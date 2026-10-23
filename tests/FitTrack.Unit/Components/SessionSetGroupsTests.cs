using FitTrack.UI.Api;
using FitTrack.UI.Components;

namespace FitTrack.Unit.Components;

/// <summary>
/// The run matrix of the history grouping: neighbors equal in exercise, repetitions and load
/// collapse together, any change breaks a run, and chronology is never reordered.
/// </summary>
public sealed class SessionSetGroupsTests
{
    private static readonly Guid CrunchId = Guid.NewGuid();

    private static readonly Guid CurlId = Guid.NewGuid();

    /// <summary>
    /// The performed set carries its load as a non-nullable decimal (bodyweight is a value,
    /// not a null), so there is no null-load case: every value groups like any other.
    /// </summary>
    private static PerformedSetResponse Set(Guid exerciseId, int repetitions, decimal loadKilograms) =>
        new(exerciseId, loadKilograms, repetitions);

    [Fact]
    public void IdenticalConsecutiveSets_FormOneRun()
    {
        var sets = new[] { Set(CrunchId, 20, 20m), Set(CrunchId, 20, 20m), Set(CrunchId, 20, 20m), Set(CrunchId, 20, 20m) };

        var runs = SessionSetGroups.Group(sets);

        var run = Assert.Single(runs);
        Assert.Equal(CrunchId, run.ExerciseId);
        Assert.Equal(20, run.Repetitions);
        Assert.Equal(20m, run.LoadKilograms);
        Assert.Equal(0, run.FirstSetIndex);
        Assert.Equal([0, 1, 2, 3], run.SetIndexes);
    }

    [Fact]
    public void AChangedLoad_BreaksIntoSameLoadRuns_OfOneSet()
    {
        var sets = new[] { Set(CrunchId, 20, 20m), Set(CrunchId, 20, 30m), Set(CrunchId, 20, 20m) };

        var runs = SessionSetGroups.Group(sets);

        Assert.Equal(3, runs.Count);
        Assert.Equal(new[] { 20m, 30m, 20m }, runs.Select(run => run.LoadKilograms));
        Assert.All(runs, run => Assert.Equal([run.FirstSetIndex], run.SetIndexes));
    }

    [Fact]
    public void ASingleSet_FormsOneRun()
    {
        var runs = SessionSetGroups.Group([Set(CrunchId, 15, 40m)]);

        var run = Assert.Single(runs);
        Assert.Equal(0, run.FirstSetIndex);
        Assert.Equal([0], run.SetIndexes);
    }

    [Fact]
    public void AnEmptySession_HasNoRuns()
    {
        Assert.Empty(SessionSetGroups.Group([]));
    }

    [Fact]
    public void TheSameExerciseApartInTheWorkout_FormsTwoSeparateRuns()
    {
        var sets = new[]
        {
            Set(CrunchId, 20, 20m),
            Set(CrunchId, 20, 20m),
            Set(CurlId, 10, 30m),
            Set(CrunchId, 20, 20m),
        };

        var runs = SessionSetGroups.Group(sets);

        Assert.Equal(3, runs.Count);
        Assert.Equal(CrunchId, runs[0].ExerciseId);
        Assert.Equal([0, 1], runs[0].SetIndexes);
        Assert.Equal(CurlId, runs[1].ExerciseId);
        Assert.Equal([2], runs[1].SetIndexes);
        Assert.Equal(3, runs[2].FirstSetIndex);
        Assert.Equal(CrunchId, runs[2].ExerciseId);
        Assert.Equal([3], runs[2].SetIndexes);
    }

    [Fact]
    public void AChangedRepetition_BreaksTheRun_EvenWithTheSameExerciseAndLoad()
    {
        var sets = new[] { Set(CrunchId, 20, 20m), Set(CrunchId, 12, 20m) };

        var runs = SessionSetGroups.Group(sets);

        Assert.Equal(2, runs.Count);
        Assert.Equal([20, 12], runs.Select(run => run.Repetitions));
    }
}

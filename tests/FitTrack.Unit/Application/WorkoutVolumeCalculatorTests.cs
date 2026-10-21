using FitTrack.Application.Dtos;
using FitTrack.Application.UseCases;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Application;

public sealed class WorkoutVolumeCalculatorTests
{
    private static readonly Guid Bench = Guid.NewGuid();
    private static readonly Guid Squat = Guid.NewGuid();
    private static readonly Guid Row = Guid.NewGuid();

    private static readonly IReadOnlyDictionary<Guid, string> Names = new Dictionary<Guid, string>
    {
        [Bench] = "Supino reto",
        [Squat] = "Agachamento",
        [Row] = "Remada",
    };

    [Fact]
    public void Calculate_ComparesEachExerciseAndTheSessionTotals()
    {
        var performed = new[]
        {
            new PerformedSetDto(Bench, 85m, 10),
            new PerformedSetDto(Bench, 85m, 10),
            new PerformedSetDto(Bench, 85m, 10),
            new PerformedSetDto(Squat, 100m, 10),
            new PerformedSetDto(Squat, 100m, 10),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 80m, PrescribedExercise.DefaultRestSeconds),
            new PrescribedExerciseDto(Squat, "Agachamento", "Pernas", 3, 10, 100m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        Assert.Equal(4550m, volume!.PerformedKilograms);
        Assert.Equal(5400m, volume.PrescribedKilograms);
        Assert.Equal(-850m, volume.DeltaKilograms);
        Assert.Equal(-15.74m, volume.DeltaPercent);

        var bench = Assert.Single(volume.Exercises, item => item.ExerciseId == Bench);
        Assert.Equal("matched", bench.Status);
        Assert.Equal(2550m, bench.PerformedKilograms);
        Assert.Equal(2400m, bench.PrescribedKilograms);
        Assert.Equal(150m, bench.DeltaKilograms);
        Assert.Equal(6.25m, bench.DeltaPercent);

        var squat = Assert.Single(volume.Exercises, item => item.ExerciseId == Squat);
        Assert.Equal(2000m, squat.PerformedKilograms);
        Assert.Equal(3000m, squat.PrescribedKilograms);
        Assert.Equal(-33.33m, squat.DeltaPercent);
    }

    [Fact]
    public void Calculate_UsesTheCanonicalNumbersOfTheSpecification()
    {
        // The example of the spec: 4 x 10 x 50 kg prescribed and four sets of 60 kg x 10 performed.
        var performed = Enumerable.Range(0, 4)
            .Select(_ => new PerformedSetDto(Bench, 60m, 10))
            .ToArray();

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 4, 10, 50m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        Assert.Equal(2400m, volume!.PerformedKilograms);
        Assert.Equal(2000m, volume.PrescribedKilograms);
        Assert.Equal(400m, volume.DeltaKilograms);
        Assert.Equal(20m, volume.DeltaPercent);
    }

    [Fact]
    public void Calculate_MarksExercisesThatWereNotPerformedOrNotPrescribed()
    {
        var performed = new[]
        {
            new PerformedSetDto(Row, 50m, 10),
            new PerformedSetDto(Row, 50m, 10),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 80m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);

        var notPerformed = Assert.Single(volume!.Exercises, item => item.ExerciseId == Bench);
        Assert.Equal("notPerformed", notPerformed.Status);
        Assert.Equal(0m, notPerformed.PerformedKilograms);
        Assert.Equal(-100m, notPerformed.DeltaPercent);

        var notPrescribed = Assert.Single(volume.Exercises, item => item.ExerciseId == Row);
        Assert.Equal("notPrescribed", notPrescribed.Status);
        Assert.Equal(1000m, notPrescribed.PerformedKilograms);
        Assert.Equal(0m, notPrescribed.PrescribedKilograms);
        Assert.Null(notPrescribed.DeltaPercent);
    }

    [Fact]
    public void Calculate_BodyweightHasNoPercentage()
    {
        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Flexão", "Peito", 3, 12, null, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate([], prescription, Names);

        Assert.NotNull(volume);
        var exercise = Assert.Single(volume!.Exercises);
        Assert.Equal("bodyweight", exercise.Status);
        Assert.Equal(0m, exercise.PrescribedKilograms);
        Assert.Null(exercise.DeltaPercent);
        Assert.Null(volume.DeltaPercent);
    }

    [Fact]
    public void Calculate_ReturnsNullWithoutAPrescription()
    {
        var volume = WorkoutVolumeCalculator.Calculate(
            [new PerformedSetDto(Bench, 80m, 10)],
            prescription: null,
            Names);

        Assert.Null(volume);
    }

    [Fact]
    public void Summarize_TotalsEverySessionAndCountsTheUncomparableOnes()
    {
        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 80m, PrescribedExercise.DefaultRestSeconds),
        };

        var matched = WorkoutVolumeCalculator.Calculate(
            [new PerformedSetDto(Bench, 85m, 10), new PerformedSetDto(Bench, 85m, 10)],
            prescription,
            Names);

        var sessions = new[]
        {
            new WorkoutSessionDto(
                Guid.NewGuid(), new DateOnly(2026, 10, 1), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                1800, Guid.NewGuid(), [new PerformedSetDto(Bench, 85m, 10), new PerformedSetDto(Bench, 85m, 10)],
                matched),
            new WorkoutSessionDto(
                Guid.NewGuid(), new DateOnly(2026, 10, 2), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                1200, null, [new PerformedSetDto(Bench, 80m, 10)]),
        };

        var summary = WorkoutVolumeCalculator.Summarize(sessions);

        Assert.Equal(2, summary.SessionCount);
        Assert.Equal(3, summary.SetCount);
        Assert.Equal(2500m, summary.PerformedKilograms);
        Assert.Equal(2400m, summary.PrescribedKilograms);
        Assert.Equal(1, summary.SessionsWithoutPrescription);
        Assert.Equal(100m, summary.DeltaKilograms);
        Assert.Equal(4.17m, summary.DeltaPercent);
    }
}



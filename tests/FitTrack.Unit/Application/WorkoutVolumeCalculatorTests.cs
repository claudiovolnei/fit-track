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
        Assert.Equal(185m, volume!.PerformedKilograms);
        Assert.Equal(180m, volume.PrescribedKilograms);
        Assert.Equal(5m, volume.DeltaKilograms);
        Assert.Equal(2.78m, volume.DeltaPercent);

        var bench = Assert.Single(volume.Exercises, item => item.ExerciseId == Bench);
        Assert.Equal("matched", bench.Status);
        Assert.Equal(85m, bench.PerformedKilograms);
        Assert.Equal(80m, bench.PrescribedKilograms);
        Assert.Equal(5m, bench.DeltaKilograms);
        Assert.Equal(6.25m, bench.DeltaPercent);

        var squat = Assert.Single(volume.Exercises, item => item.ExerciseId == Squat);
        Assert.Equal(100m, squat.PerformedKilograms);
        Assert.Equal(100m, squat.PrescribedKilograms);
        Assert.Equal(0m, squat.DeltaKilograms);
        Assert.Equal(0m, squat.DeltaPercent);
    }

    [Fact]
    public void Calculate_UsesTheCanonicalNumbersOfTheSpecification()
    {
        // The example of the spec: 4 x 10 x 50 kg prescribed and four sets of 60 kg performed —
        // a load increase of 10 kg, which is what the athlete raised, nothing more.
        var performed = Enumerable.Range(0, 4)
            .Select(_ => new PerformedSetDto(Bench, 60m, 10))
            .ToArray();

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 4, 10, 50m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        Assert.Equal(60m, volume!.PerformedKilograms);
        Assert.Equal(50m, volume.PrescribedKilograms);
        Assert.Equal(10m, volume.DeltaKilograms);
        Assert.Equal(20m, volume.DeltaPercent);
    }

    [Fact]
    public void Calculate_SumsTheLoadIncreaseExerciseByExercise()
    {
        // The example of the owner: one exercise raises 40 -> 48 (+8) and another 30 -> 32 (+2),
        // so the session shows +10 kg — the sum of the load differences, not of tonnage.
        var performed = new[]
        {
            new PerformedSetDto(Bench, 48m, 10),
            new PerformedSetDto(Bench, 48m, 10),
            new PerformedSetDto(Bench, 48m, 10),
            new PerformedSetDto(Squat, 32m, 8),
            new PerformedSetDto(Squat, 32m, 8),
            new PerformedSetDto(Squat, 32m, 8),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 40m, PrescribedExercise.DefaultRestSeconds),
            new PrescribedExerciseDto(Squat, "Agachamento", "Pernas", 3, 8, 30m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        Assert.Equal(80m, volume!.PerformedKilograms);
        Assert.Equal(70m, volume.PrescribedKilograms);
        Assert.Equal(10m, volume.DeltaKilograms);
        Assert.Equal(14.29m, volume.DeltaPercent);
    }

    [Fact]
    public void Calculate_AveragesTheSetsOfTheSameExercise()
    {
        var performed = new[]
        {
            new PerformedSetDto(Bench, 40m, 10),
            new PerformedSetDto(Bench, 50m, 10),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 40m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        var bench = Assert.Single(volume!.Exercises);
        Assert.Equal("matched", bench.Status);
        Assert.Equal(45m, bench.PerformedKilograms);
        Assert.Equal(5m, bench.DeltaKilograms);
        Assert.Equal(12.5m, bench.DeltaPercent);
    }

    [Fact]
    public void Calculate_ExcludesZeroLoadSetsFromTheAverage()
    {
        // Bodyweight sets carry no load to raise, so they do not dilute the average.
        var performed = new[]
        {
            new PerformedSetDto(Bench, 40m, 10),
            new PerformedSetDto(Bench, 0m, 10),
            new PerformedSetDto(Bench, 50m, 10),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 3, 10, 40m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        var bench = Assert.Single(volume!.Exercises);
        Assert.Equal(45m, bench.PerformedKilograms);
    }

    [Fact]
    public void Calculate_WeightsThePrescribedLoadBySets()
    {
        // Two prescription rows for the same exercise: 2 sets @ 40 kg and 2 sets @ 60 kg — the
        // representative load per set is 50 kg.
        var performed = new[]
        {
            new PerformedSetDto(Bench, 55m, 10),
            new PerformedSetDto(Bench, 55m, 10),
        };

        var prescription = new[]
        {
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 2, 10, 40m, PrescribedExercise.DefaultRestSeconds),
            new PrescribedExerciseDto(Bench, "Supino reto", "Peito", 2, 10, 60m, PrescribedExercise.DefaultRestSeconds),
        };

        var volume = WorkoutVolumeCalculator.Calculate(performed, prescription, Names);

        Assert.NotNull(volume);
        var bench = Assert.Single(volume!.Exercises);
        Assert.Equal(50m, bench.PrescribedKilograms);
        Assert.Equal(55m, bench.PerformedKilograms);
        Assert.Equal(5m, bench.DeltaKilograms);
        Assert.Equal(10m, bench.DeltaPercent);
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
        Assert.Equal(0m, volume!.PerformedKilograms);
        Assert.Equal(80m, volume.PrescribedKilograms);
        Assert.Equal(-80m, volume.DeltaKilograms);
        Assert.Equal(-100m, volume.DeltaPercent);

        var notPerformed = Assert.Single(volume.Exercises, item => item.ExerciseId == Bench);
        Assert.Equal("notPerformed", notPerformed.Status);
        Assert.Equal(0m, notPerformed.PerformedKilograms);
        Assert.Equal(80m, notPerformed.PrescribedKilograms);
        Assert.Equal(-100m, notPerformed.DeltaPercent);

        var notPrescribed = Assert.Single(volume.Exercises, item => item.ExerciseId == Row);
        Assert.Equal("notPrescribed", notPrescribed.Status);
        Assert.Equal(50m, notPrescribed.PerformedKilograms);
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
        Assert.Equal(85m, summary.PerformedKilograms);
        Assert.Equal(80m, summary.PrescribedKilograms);
        Assert.Equal(1, summary.SessionsWithoutPrescription);
        Assert.Equal(5m, summary.DeltaKilograms);
        Assert.Equal(6.25m, summary.DeltaPercent);
    }
}

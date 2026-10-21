using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Domain;

public sealed class WorkoutSeriesTests
{
    private static readonly UserId Trainer = UserId.New();
    private static readonly ExerciseId Bench = ExerciseId.New();
    private static readonly ExerciseId Row = ExerciseId.New();

    [Fact]
    public void Create_WithoutTrainer_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = WorkoutSeries.Create(WorkoutSeriesId.New(), new WorkoutSeriesName("Hipertrofia"), new UserId(Guid.Empty)));

        Assert.Equal("workout_series.trainer.required", exception.Code);
    }

    [Fact]
    public void Name_TooShort_Fails()
    {
        var exception = Assert.Throws<DomainException>(() => _ = new WorkoutSeriesName(" A "));

        Assert.Equal("workout_series.name.required", exception.Code);
    }

    [Fact]
    public void Name_TooLong_Fails()
    {
        var tooLong = new string('x', WorkoutSeriesName.MaxLength + 1);

        Assert.Throws<DomainException>(() => _ = new WorkoutSeriesName(tooLong));
    }

    [Fact]
    public void AddWorkout_LabelsThemFromAAndNumbersFromOne()
    {
        var series = Create();

        var first = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito e Tríceps"), [Prescribed(Bench)]);
        var second = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Costas e Bíceps"), [Prescribed(Row)]);

        Assert.Equal(1, first.Position);
        Assert.Equal("A", first.Label);
        Assert.Equal(2, second.Position);
        Assert.Equal("B", second.Label);
    }

    [Fact]
    public void AddWorkout_WithoutExercises_Fails()
    {
        var series = Create();

        var exception = Assert.Throws<DomainException>(() =>
            series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), []));

        Assert.Equal("workout_series.workout.exercises.empty", exception.Code);
    }

    [Fact]
    public void AddWorkout_WithTheSameExerciseTwice_Fails()
    {
        var series = Create();

        var exception = Assert.Throws<DomainException>(() =>
            series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), [Prescribed(Bench), Prescribed(Bench)]));

        Assert.Equal("workout_series.exercise.duplicate", exception.Code);
    }

    [Fact]
    public void AddWorkout_BeyondFive_Fails()
    {
        var series = Create();

        for (var index = 1; index <= WorkoutSeries.MaxWorkouts; index++)
        {
            series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName($"Treino {index}"), [Prescribed(Bench)]);
        }

        var exception = Assert.Throws<DomainException>(() =>
            series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Treino 6"), [Prescribed(Row)]));

        Assert.Equal("workout_series.workout.limit_reached", exception.Code);
    }

    [Fact]
    public void ChangeWorkout_ReplacesTheNameAndTheExercises()
    {
        var series = Create();
        var workout = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), [Prescribed(Bench)]);

        series.ChangeWorkout(workout.Id, new WorkoutName("Costas"), [Prescribed(Row, sets: 4, repetitions: 8, load: 60m)]);

        Assert.Equal("Costas", workout.Name.Value);
        var exercise = Assert.Single(workout.Exercises);
        Assert.Equal(Row, exercise.ExerciseId);
        Assert.Equal(4, exercise.Sets);
        Assert.Equal(8, exercise.Repetitions);
        Assert.Equal(60m, exercise.LoadKilograms);
    }

    [Fact]
    public void ChangeWorkout_ToEmpty_Fails()
    {
        var series = Create();
        var workout = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), [Prescribed(Bench)]);

        var exception = Assert.Throws<DomainException>(() =>
            series.ChangeWorkout(workout.Id, new WorkoutName("Peito"), []));

        Assert.Equal("workout_series.workout.exercises.empty", exception.Code);
        Assert.Single(workout.Exercises);
    }

    [Fact]
    public void ChangeWorkout_UnknownWorkout_Fails()
    {
        var series = Create();

        var exception = Assert.Throws<DomainException>(() =>
            series.ChangeWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), [Prescribed(Bench)]));

        Assert.Equal("workout_series.workout.not_found", exception.Code);
    }

    [Fact]
    public void RemoveWorkout_RenumbersTheRestSoTheLabelsStayContiguous()
    {
        var series = Create();
        var first = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Peito"), [Prescribed(Bench)]);
        var second = series.AddWorkout(WorkoutSeriesWorkoutId.New(), new WorkoutName("Costas"), [Prescribed(Row)]);

        series.RemoveWorkout(first.Id);

        var remaining = Assert.Single(series.Workouts);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(1, remaining.Position);
        Assert.Equal("A", remaining.Label);
    }

    [Fact]
    public void RemoveWorkout_UnknownWorkout_Fails()
    {
        var series = Create();

        var exception = Assert.Throws<DomainException>(() =>
            series.RemoveWorkout(WorkoutSeriesWorkoutId.New()));

        Assert.Equal("workout_series.workout.not_found", exception.Code);
    }

    [Fact]
    public void Rename_ChangesTheName()
    {
        var series = Create();

        series.Rename(new WorkoutSeriesName("Força A/B"));

        Assert.Equal("Força A/B", series.Name.Value);
    }

    [Fact]
    public void PrescribedExercise_EmptyExercise_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = new PrescribedExercise(new ExerciseId(Guid.Empty), 3, 12, 40m, PrescribedExercise.DefaultRestSeconds));

        Assert.Equal("workout_series.exercise.required", exception.Code);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(21, 12)]
    [InlineData(3, 0)]
    [InlineData(3, 101)]
    public void PrescribedExercise_OutOfRange_Fails(int sets, int repetitions)
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = new PrescribedExercise(Bench, sets, repetitions, 40m, PrescribedExercise.DefaultRestSeconds));

        Assert.Equal("workout_series.exercise.prescription.invalid", exception.Code);
    }

    [Fact]
    public void PrescribedExercise_NegativeLoad_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = new PrescribedExercise(Bench, 3, 12, -1m, PrescribedExercise.DefaultRestSeconds));

        Assert.Equal("workout_series.exercise.prescription.invalid", exception.Code);
    }

    [Fact]
    public void PrescribedExercise_WithoutLoad_IsAllowed()
    {
        var exercise = new PrescribedExercise(Bench, 3, 12, loadKilograms: null, restSeconds: PrescribedExercise.DefaultRestSeconds);

        Assert.Null(exercise.LoadKilograms);
    }

    private static WorkoutSeries Create() =>
        WorkoutSeries.Create(WorkoutSeriesId.New(), new WorkoutSeriesName("Hipertrofia A/B/C"), Trainer);

    private static PrescribedExercise Prescribed(
        ExerciseId exercise,
        int sets = 3,
        int repetitions = 12,
        decimal? load = 40m) =>
        new(exercise, sets, repetitions, load, PrescribedExercise.DefaultRestSeconds);
}


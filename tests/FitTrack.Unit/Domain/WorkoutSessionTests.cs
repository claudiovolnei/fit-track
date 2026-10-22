using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Domain;

public sealed class WorkoutSessionTests
{
    private static readonly UserId AnyUser = UserId.New();
    private static readonly WorkoutDate AnyDate = new(new DateOnly(2026, 1, 5));
    private static readonly DateTimeOffset AnyStart = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_WithoutUser_Fails()
    {
        Assert.Throws<DomainException>(() =>
            _ = WorkoutSession.Start(WorkoutSessionId.New(), new UserId(Guid.Empty), AnyDate, AnyStart));
    }

    [Fact]
    public void RegisterSet_AddsTheSetToTheAggregate()
    {
        var session = Start();
        var exercise = ExerciseId.New();

        session.RegisterSet(exercise, new Load(80m), new Repetitions(10));

        var set = Assert.Single(session.PerformedSets);
        Assert.Equal(exercise, set.ExerciseId);
        Assert.Equal(80m, set.Load.Kilograms);
        Assert.Equal(10, set.Repetitions.Value);
    }

    [Fact]
    public void RegisterSet_ZeroRepetitions_Fails()
    {
        var session = Start();

        Assert.Throws<DomainException>(() =>
            session.RegisterSet(ExerciseId.New(), new Load(80m), new Repetitions(0)));
    }

    [Fact]
    public void RegisterSet_EmptyExercise_Fails()
    {
        var session = Start();

        Assert.Throws<DomainException>(() =>
            session.RegisterSet(new ExerciseId(Guid.Empty), new Load(80m), new Repetitions(10)));
    }

    [Fact]
    public void StartFromPlannedWorkout_KeepsTheWorkoutItCameFrom()
    {
        var workout = WorkoutSeriesWorkoutId.New();

        var session = WorkoutSession.StartFromPlannedWorkout(
            WorkoutSessionId.New(),
            AnyUser,
            AnyDate,
            AnyStart,
            workout);

        Assert.Equal(workout, session.PlannedWorkoutId);
        Assert.Equal(AnyStart, session.StartedAt);
        Assert.True(session.IsRunning);
        Assert.False(session.HasSets);
    }

    [Fact]
    public void Duration_WhileRunning_CountsUpToTheInstantAsked()
    {
        var session = Start();

        Assert.Equal(TimeSpan.FromMinutes(15), session.Duration(AnyStart.AddMinutes(15)));
    }

    [Fact]
    public void Duration_AfterFinish_FreezesAtTheFinish()
    {
        var session = Start();

        session.Finish(AnyStart.AddMinutes(42));

        Assert.False(session.IsRunning);
        Assert.Equal(AnyStart.AddMinutes(42), session.FinishedAt);
        Assert.Equal(TimeSpan.FromMinutes(42), session.Duration(AnyStart.AddHours(3)));
    }

    [Fact]
    public void Finish_Twice_Fails()
    {
        var session = Start();
        session.Finish(AnyStart.AddMinutes(10));

        var exception = Assert.Throws<DomainException>(() => session.Finish(AnyStart.AddMinutes(20)));

        Assert.Equal("session.finish.already_finished", exception.Code);
    }

    [Fact]
    public void Finish_BeforeTheStart_Fails()
    {
        var session = Start();

        var exception = Assert.Throws<DomainException>(() => session.Finish(AnyStart.AddMinutes(-1)));

        Assert.Equal("session.finish.before_start", exception.Code);
    }

    [Fact]
    public void RegisterSet_AfterFinish_Fails()
    {
        var session = Start();
        session.Finish(AnyStart.AddMinutes(10));

        var exception = Assert.Throws<DomainException>(() =>
            session.RegisterSet(ExerciseId.New(), new Load(80m), new Repetitions(10)));

        Assert.Equal("session.set.after_finish", exception.Code);
    }

    [Fact]
    public void RegisterSet_TicksTheExerciseAsDone()
    {
        var session = Start();
        var exercise = ExerciseId.New();

        session.RegisterSet(exercise, new Load(80m), new Repetitions(10));

        Assert.True(session.IsExerciseCompleted(exercise));
        Assert.Equal([exercise], session.CompletedExercises);
    }

    [Fact]
    public void MarkExerciseCompleted_TicksTheExerciseOnce()
    {
        var session = Start();
        var exercise = ExerciseId.New();

        session.MarkExerciseCompleted(exercise);
        session.MarkExerciseCompleted(exercise);

        Assert.Equal([exercise], session.CompletedExercises);
    }

    [Fact]
    public void MarkExerciseCompleted_AfterFinish_Fails()
    {
        var session = Start();
        session.Finish(AnyStart.AddMinutes(10));

        var exception = Assert.Throws<DomainException>(() =>
            session.MarkExerciseCompleted(ExerciseId.New()));

        Assert.Equal("session.exercise.after_finish", exception.Code);
    }

    private static WorkoutSession Start() =>
        WorkoutSession.Start(WorkoutSessionId.New(), AnyUser, AnyDate, AnyStart);

    [Fact]
    public void Complete_RecordsTheSetsTheLevelAndTheNote()
    {
        var session = Start();
        var exercise = ExerciseId.New();

        session.Complete(
            [new PerformedExercise(exercise, new Load(80m), new Repetitions(10), 3)],
            WorkoutIntensity.Intenso,
            new WorkoutNotes("Senti o ombro."),
            AnyStart.AddMinutes(42));

        Assert.False(session.IsRunning);
        Assert.Equal(3, session.PerformedSets.Count);
        Assert.All(session.PerformedSets, set => Assert.Equal(exercise, set.ExerciseId));
        Assert.True(session.IsExerciseCompleted(exercise));
        Assert.Equal(WorkoutIntensity.Intenso, session.Intensity);
        Assert.Equal("Senti o ombro.", session.Notes!.Value.Value);
    }

    [Fact]
    public void Complete_Twice_Fails()
    {
        var session = Start();
        session.Complete([], WorkoutIntensity.Moderado, null, AnyStart.AddMinutes(10));

        var exception = Assert.Throws<DomainException>(() =>
            session.Complete([], WorkoutIntensity.Moderado, null, AnyStart.AddMinutes(20)));

        Assert.Equal("session.finish.already_finished", exception.Code);
    }

    [Fact]
    public void Complete_BeforeTheStart_Fails()
    {
        var session = Start();

        var exception = Assert.Throws<DomainException>(() =>
            session.Complete([], WorkoutIntensity.Moderado, null, AnyStart.AddMinutes(-1)));

        Assert.Equal("session.finish.before_start", exception.Code);
    }

    [Fact]
    public void Complete_WithoutANote_LeavesTheSessionWithoutOne()
    {
        var session = Start();

        session.Complete(
            [new PerformedExercise(ExerciseId.New(), new Load(80m), new Repetitions(10), 3)],
            WorkoutIntensity.Leve,
            null,
            AnyStart.AddMinutes(42));

        // The note is optional: without it the session carries no note at all.
        Assert.Null(session.Notes);
        Assert.Equal(WorkoutIntensity.Leve, session.Intensity);
    }

    [Fact]
    public void WorkoutNotes_AtTheLimit_IsAccepted()
    {
        var notes = new WorkoutNotes(new string('a', WorkoutNotes.MaxLength));

        Assert.Equal(WorkoutNotes.MaxLength, notes.Value.Length);
    }

    [Fact]
    public void PerformedExercise_WithoutSets_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = new PerformedExercise(ExerciseId.New(), new Load(80m), new Repetitions(10), 0));

        Assert.Equal("set.count.not_positive", exception.Code);
    }

    [Fact]
    public void WorkoutNotes_LongerThanTheLimit_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = new WorkoutNotes(new string('a', WorkoutNotes.MaxLength + 1)));

        Assert.Equal("workout_session.notes.too_long", exception.Code);
    }

    [Fact]
    public void WorkoutIntensity_HasOneStableNumberPerLevel()
    {
        // The enum is stored as a number: reordering the members must never change what an old row means.
        Assert.Equal(1, (int)WorkoutIntensity.Leve);
        Assert.Equal(2, (int)WorkoutIntensity.Medio);
        Assert.Equal(3, (int)WorkoutIntensity.Moderado);
        Assert.Equal(4, (int)WorkoutIntensity.Intenso);
        Assert.Equal(5, (int)WorkoutIntensity.MuitoIntenso);
    }
}

using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Adapters.Out.Persistence.Queries;
using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Integration;

/// <summary>
/// The level and the note of a workout have to survive the trip to the database and back: the card
/// in the history reads them from the row, so a column that loses the value takes the athlete's
/// account of the session with it.
/// <para>
/// It needs no running database: the mapper and the projection are pure, so the round trip is
/// proved here, where the suite always runs.
/// </para>
/// </summary>
public sealed class SessionIntensityPersistenceTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FinishedAt = StartedAt.AddMinutes(42);

    [Fact]
    public void TheLevelAndTheNoteSurviveTheRoundTrip()
    {
        var session = Finished(WorkoutIntensity.Intenso, new WorkoutNotes("Senti o ombro."));

        var record = WorkoutSessionMapper.ToRecord(session);
        var back = WorkoutSessionMapper.ToDomain(record);

        Assert.Equal(WorkoutIntensity.Intenso, back.Intensity);
        Assert.Equal("Senti o ombro.", back.Notes!.Value.Value);

        // And the row the next write builds still carries them.
        var again = WorkoutSessionMapper.ToRecord(back);

        Assert.Equal((int)WorkoutIntensity.Intenso, again.Intensity);
        Assert.Equal("Senti o ombro.", again.Notes);
    }

    [Fact]
    public void TheRowHoldsTheNumberOfTheLevel()
    {
        // The level is stored as the number of the enum, with an explicit value per level, so a row
        // written before the enum was reordered still means the same thing.
        var record = WorkoutSessionMapper.ToRecord(Finished(WorkoutIntensity.MuitoIntenso, null));

        Assert.Equal(5, record.Intensity);
    }

    [Fact]
    public void TheLevelAndTheNoteComeBackInTheReadModel()
    {
        var record = WorkoutSessionMapper.ToRecord(
            Finished(WorkoutIntensity.Moderado, new WorkoutNotes("Melhor hoje.")));

        var dto = Row(record).ToDto(FinishedAt.AddMinutes(1));

        Assert.Equal("moderado", dto.Intensity);
        Assert.Equal("Melhor hoje.", dto.Notes);
    }

    [Fact]
    public void ALegacySessionWithoutTheLevelStaysWithoutOne()
    {
        // A session finished before the athlete started answering the level: the row carries no
        // level and no note, and reading it back must not invent either.
        var record = WorkoutSessionMapper.ToRecord(Legacy());

        Assert.Null(record.Intensity);
        Assert.Null(record.Notes);

        var back = WorkoutSessionMapper.ToDomain(record);

        Assert.Null(back.Intensity);
        Assert.Null(back.Notes);

        var dto = Row(record).ToDto(FinishedAt.AddMinutes(1));

        Assert.Null(dto.Intensity);
        Assert.Null(dto.Notes);
    }

    [Fact]
    public void ARunningSessionCarriesNoLevelYet()
    {
        var session = WorkoutSession.Start(
            WorkoutSessionId.New(),
            new UserId(Guid.NewGuid()),
            new WorkoutDate(new DateOnly(2026, 1, 5)),
            StartedAt);

        var record = WorkoutSessionMapper.ToRecord(session);

        Assert.Null(record.FinishedAt);
        Assert.Null(record.Intensity);
        Assert.Null(record.Notes);
    }

    /// <summary>A session closed with the level and the note the athlete gave it.</summary>
    private static WorkoutSession Finished(WorkoutIntensity intensity, WorkoutNotes? notes)
    {
        var session = Running();

        session.Complete(
            [new PerformedExercise(ExerciseId.New(), [(new Load(40m), new Repetitions(10)), (new Load(40m), new Repetitions(10)), (new Load(40m), new Repetitions(10))])],
            intensity,
            notes,
            FinishedAt);

        return session;
    }

    /// <summary>
    /// A session closed before the level existed: it has a finish, but neither a level nor a note.
    /// </summary>
    private static WorkoutSession Legacy()
    {
        var session = Running();

        session.Finish(FinishedAt);

        return session;
    }

    private static WorkoutSession Running() =>
        WorkoutSession.Start(
            WorkoutSessionId.New(),
            new UserId(Guid.NewGuid()),
            new WorkoutDate(new DateOnly(2026, 1, 5)),
            StartedAt);

    /// <summary>The row the read side builds out of the stored session.</summary>
    private static WorkoutSessionRow Row(WorkoutSessionRecord record) =>
        new(
            record.Id,
            record.WorkoutDate,
            record.StartedAt,
            record.FinishedAt,
            record.WorkoutSeriesWorkoutId,
            [],
            [],
            record.Intensity is { } intensity ? (WorkoutIntensity)intensity : null,
            record.Notes);
}

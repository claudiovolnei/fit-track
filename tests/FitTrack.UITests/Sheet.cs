using FitTrack.UI.Api;

namespace FitTrack.UI.Tests;

/// <summary>
/// The sheet the athlete's screen reads: a series with one workout in progress and the exercises
/// prescribed for it. The tests describe a workout with it, so each one says what it is about.
/// </summary>
internal static class Sheet
{
    public static readonly Guid SeriesId = Guid.NewGuid();

    public static readonly Guid WorkoutId = Guid.NewGuid();

    public static readonly Guid SessionId = Guid.NewGuid();

    public static readonly Guid BenchId = Guid.NewGuid();

    public static readonly Guid RowId = Guid.NewGuid();

    public static readonly Guid TrainerId = Guid.NewGuid();

    /// <summary>
    /// A series with one workout in progress: the athlete started it and the server holds nothing
    /// but the clock — the marks live on the screen.
    /// </summary>
    public static MyWorkoutSeriesResponse Running(
        int sets = 4,
        int repetitions = 10,
        decimal? loadKilograms = 40m,
        bool selfAuthored = false) =>
        new(
            new WorkoutSeriesResponse(
                SeriesId,
                "Hipertrofia A/B/C",
                selfAuthored ? BenchId : TrainerId,
                BenchId,
                [
                    new WorkoutSeriesWorkoutResponse(
                        WorkoutId,
                        1,
                        "A",
                        "Peito e Tríceps",
                        [
                            new PrescribedExerciseResponse(
                                BenchId,
                                "Supino reto",
                                "Peito",
                                sets,
                                repetitions,
                                loadKilograms,
                                60),
                            new PrescribedExerciseResponse(
                                RowId,
                                "Remada curvada",
                                "Costas",
                                sets,
                                repetitions,
                                loadKilograms,
                                60),
                        ],
                        new TodayWorkoutResponse(
                            SessionId,
                            DateTimeOffset.UtcNow.AddMinutes(-10),
                            null,
                            600,
                            true,
                            [])),
                ]),
            selfAuthored ? "Atleta" : "Personal trainer",
            DateTimeOffset.UtcNow.AddDays(30),
            30,
            false)
        {
            IsSelfAuthored = selfAuthored,
        };

    /// <summary>The catalog the screen reads alongside the series.</summary>
    public static IReadOnlyList<ExerciseResponse> Catalog =>
    [
        new(BenchId, "Supino reto", Guid.NewGuid(), "Peito", true),
        new(RowId, "Remada curvada", Guid.NewGuid(), "Costas", true),
    ];

    /// <summary>
    /// A finished chest workout made of identical sets — the shape the history card groups
    /// into one collapsed run. Nothing but the id changes between the sessions of a history.
    /// </summary>
    public static WorkoutSessionResponse ChestWorkout(
        Guid id,
        int sets = 4,
        int repetitions = 20,
        decimal loadKilograms = 20m,
        string? notes = null) =>
        new(
            id,
            new DateOnly(2026, 1, 5),
            DateTimeOffset.UtcNow.AddMinutes(-90),
            DateTimeOffset.UtcNow.AddMinutes(-48),
            2520,
            null,
            [.. Enumerable.Range(0, sets).Select(_ => new PerformedSetResponse(BenchId, loadKilograms, repetitions))])
        {
            Notes = notes,
        };

    /// <summary>One page of the history in the shape the client unwraps.</summary>
    public static WorkoutHistoryResponse History(
        IReadOnlyList<WorkoutSessionResponse> sessions,
        int page,
        int totalCount,
        int pageSize = 10)
    {
        var totalPages = pageSize <= 0 || totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new WorkoutHistoryResponse(
            sessions,
            page,
            pageSize,
            totalCount,
            totalPages,
            page > 1,
            page < totalPages,
            Summary: null);
    }

    /// <summary>The athlete's roster entry the trainer history screen reads first.</summary>
    public static IReadOnlyList<AthleteResponse> Roster(Guid userId) =>
    [
        new(userId, "athlete@fittrack.local", null, null, 0),
    ];
}

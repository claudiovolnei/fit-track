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
}

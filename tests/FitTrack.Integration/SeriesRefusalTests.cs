using System.Net;
using System.Net.Http.Json;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Integration.Contracts;
using FitTrack.TestCases;

namespace FitTrack.Integration;

/// <summary>
/// Drives the refusals of the series slice over HTTP against PostgreSQL. The unit suite drives the very
/// same table through the use cases over the in-memory fakes, so this is where a fake and an adapter that
/// stopped agreeing shows up: both suites read <see cref="SeriesRefusals"/>, and the codes have to match
/// on both sides of the hexagon.
/// </summary>
public sealed class SeriesRefusalTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    /// <summary>A prescription with nothing in it, for the arm that proves an empty list is refused.</summary>
    private static readonly Task<IReadOnlyList<PrescribedExerciseRequest>> NoPrescription =
        Task.FromResult<IReadOnlyList<PrescribedExerciseRequest>>([]);

    [Theory]
    [MemberData(nameof(SeriesRefusals.Refusals), MemberType = typeof(SeriesRefusals))]
    public async Task EveryRefusal_ComesBackWithTheAgreedCodeOverPostgres(string scenario, string code)
    {
        var response = await RefuseAsync(scenario);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, await response.ReadProblemTitleAsync());
    }

    /// <summary>
    /// The refusal the API answers for one scenario, arranged over the real edge and the real database.
    /// </summary>
    private async Task<HttpResponseMessage> RefuseAsync(string scenario) => scenario switch
    {
        SeriesRefusals.SeriesNameRequired => await WhenSignedInAsync(trainer =>
            trainer.PostAsJsonAsync("/workout-series", new SetWorkoutSeriesNameRequest(" "))),

        SeriesRefusals.SeriesNameDuplicate => await WhenSignedInAsync(DuplicateNameAsync),

        SeriesRefusals.WorkoutNameRequired => await AtANewWorkoutAsync(" ", OneFreshExerciseAsync),

        SeriesRefusals.WorkoutPrescriptionEmpty => await AtANewWorkoutAsync("Peito e Tríceps", _ => NoPrescription),

        SeriesRefusals.WorkoutPrescriptionDuplicateExercise => await AtANewWorkoutAsync(
            "Peito e Tríceps",
            async trainer =>
            {
                var exercise = await SeriesArrange.NewExerciseAsync(trainer);

                return [Prescriptions.For(exercise.Id), Prescriptions.For(exercise.Id)];
            }),

        SeriesRefusals.WorkoutPrescriptionOutOfRange => await AtANewWorkoutAsync(
            "Peito e Tríceps",
            async trainer => [Prescriptions.For((await SeriesArrange.NewExerciseAsync(trainer)).Id, sets: 0)]),

        SeriesRefusals.WorkoutLimitReached => await WhenSignedInAsync(SixthWorkoutAsync),

        SeriesRefusals.WorkoutRemovedTwice => await WhenSignedInAsync(RemovingTheSameWorkoutTwiceAsync),

        SeriesRefusals.AssignmentExpired => await WhenSignedInAsync(async trainer =>
        {
            var athlete = await factory.CreateAthleteAsync();
            var exercise = await SeriesArrange.NewExerciseAsync(trainer);
            var workoutId = await SeriesArrange.AssignSeriesAsync(
                trainer, athlete.UserId, Prescriptions.For(exercise.Id));

            await factory.ExpireSeriesAsync(athlete.UserId);

            return await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
        }),

        SeriesRefusals.SetOutsidePrescription => await WhenSignedInAsync(async trainer =>
        {
            var athlete = await factory.CreateAthleteAsync();
            var prescribed = await SeriesArrange.NewExerciseAsync(trainer);
            var workoutId = await SeriesArrange.AssignSeriesAsync(
                trainer, athlete.UserId, Prescriptions.For(prescribed.Id));

            var started = await athlete.Client.PostAsJsonAsync("/sessions", new StartWorkoutRequest(workoutId));
            var session = (await started.Content.ReadFromJsonAsync<WorkoutSessionResponse>())!;

            return await athlete.Client.PostAsJsonAsync(
                $"/sessions/{session.Id}/sets",
                new LogPerformedSetRequest(Guid.NewGuid(), 40m));
        }),

        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown series refusal."),
    };

    /// <summary>Runs one intention with the seeded trainer signed in.</summary>
    private async Task<HttpResponseMessage> WhenSignedInAsync(Func<HttpClient, Task<HttpResponseMessage>> intention)
    {
        var trainer = await factory.TrainerAsync();

        return await intention(trainer);
    }

    /// <summary>The add intention pointed at a brand-new series, so an arm states only the broken input.</summary>
    private async Task<HttpResponseMessage> AtANewWorkoutAsync(
        string name,
        Func<HttpClient, Task<IReadOnlyList<PrescribedExerciseRequest>>> prescription)
    {
        var trainer = await factory.TrainerAsync();
        var series = await trainer.CreateSeriesAsync($"Série {Guid.NewGuid():N}");

        return await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/workouts",
            new SetSeriesWorkoutRequest(name, await prescription(trainer)));
    }

    /// <summary>Prescribes one fresh catalog exercise, the ordinary input an arm needs to reach its rule.</summary>
    private static async Task<IReadOnlyList<PrescribedExerciseRequest>> OneFreshExerciseAsync(HttpClient trainer) =>
        [Prescriptions.For((await SeriesArrange.NewExerciseAsync(trainer)).Id)];

    private static async Task<HttpResponseMessage> DuplicateNameAsync(HttpClient trainer)
    {
        var name = $"Recusa {Guid.NewGuid():N}";

        await trainer.CreateSeriesAsync(name);

        return await trainer.PostAsJsonAsync("/workout-series", new SetWorkoutSeriesNameRequest(name));
    }

    private static async Task<HttpResponseMessage> SixthWorkoutAsync(HttpClient trainer)
    {
        var exercise = await SeriesArrange.NewExerciseAsync(trainer);
        var series = await trainer.CreateSeriesAsync($"Recusa {Guid.NewGuid():N}");

        for (var index = 1; index <= WorkoutSeries.MaxWorkouts; index++)
        {
            await trainer.AddWorkoutAsync(series.Id, $"Treino {index}", Prescriptions.For(exercise.Id));
        }

        return await trainer.PostAsJsonAsync(
            $"/workout-series/{series.Id}/workouts",
            new SetSeriesWorkoutRequest("Treino 6", [Prescriptions.For(exercise.Id)]));
    }

    private static async Task<HttpResponseMessage> RemovingTheSameWorkoutTwiceAsync(HttpClient trainer)
    {
        var exercise = await SeriesArrange.NewExerciseAsync(trainer);
        var series = await trainer.CreateSeriesAsync($"Recusa {Guid.NewGuid():N}");
        var workout = await trainer.AddWorkoutAsync(series.Id, "Peito e Tríceps", Prescriptions.For(exercise.Id));

        await trainer.DeleteAsync($"/workout-series/{series.Id}/workouts/{workout.Id}");

        return await trainer.DeleteAsync($"/workout-series/{series.Id}/workouts/{workout.Id}");
    }
}

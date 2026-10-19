using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the planning slice. The trainer side carries the personal trainer policy; the
/// athlete side only requires an authenticated account, and each use case re-states who may act.
/// </summary>
internal static class WorkoutSeriesEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var athletes = app.MapGroup("/athletes")
            .WithTags("Athletes")
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        athletes.MapGet("/", ListAthletesAsync);
        athletes.MapGet("/{userId:guid}/workouts", ListAthleteWorkoutsAsync);

        var series = app.MapGroup("/workout-series")
            .WithTags("Workout series")
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        series.MapGet("/", ListSeriesAsync);
        series.MapPost("/", CreateSeriesAsync);
        series.MapGet("/{seriesId:guid}", GetSeriesAsync);
        series.MapPut("/{seriesId:guid}", RenameSeriesAsync);
        series.MapPost("/{seriesId:guid}/workouts", AddWorkoutAsync);
        series.MapPut("/{seriesId:guid}/workouts/{workoutId:guid}", ChangeWorkoutAsync);
        series.MapDelete("/{seriesId:guid}/workouts/{workoutId:guid}", RemoveWorkoutAsync);
        series.MapPost("/{seriesId:guid}/assignments", AssignAsync);

        // The athlete reads their own series through a singular route: no id, no way to ask for a
        // series that is not theirs.
        app.MapGet("/workout-series/mine", GetMySeriesAsync)
            .WithTags("Workout series")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListAthletesAsync(
        IListAthletes useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> ListAthleteWorkoutsAsync(
        Guid userId,
        IListAthleteWorkouts useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(new ListAthleteWorkoutsCommand(userId), cancellationToken));

    private static async Task<IResult> ListSeriesAsync(
        IListWorkoutSeries useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> CreateSeriesAsync(
        SetWorkoutSeriesNameRequest request,
        ICreateWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        var series = await useCase.ExecuteAsync(new CreateWorkoutSeriesCommand(request.Name), cancellationToken);

        return Results.Created($"/workout-series/{series.Id}", series);
    }

    private static async Task<IResult> GetSeriesAsync(
        Guid seriesId,
        IGetWorkoutSeries useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(seriesId, cancellationToken));

    private static async Task<IResult> RenameSeriesAsync(
        Guid seriesId,
        SetWorkoutSeriesNameRequest request,
        IRenameWorkoutSeries useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(
            new RenameWorkoutSeriesCommand(seriesId, request.Name),
            cancellationToken));

    private static async Task<IResult> AddWorkoutAsync(
        Guid seriesId,
        SetSeriesWorkoutRequest request,
        IAddSeriesWorkout useCase,
        CancellationToken cancellationToken)
    {
        var workout = await useCase.ExecuteAsync(
            new AddSeriesWorkoutCommand(seriesId, request.Name, Prescription(request.Exercises)),
            cancellationToken);

        return Results.Created($"/workout-series/{seriesId}/workouts/{workout.Id}", workout);
    }

    private static async Task<IResult> ChangeWorkoutAsync(
        Guid seriesId,
        Guid workoutId,
        SetSeriesWorkoutRequest request,
        IChangeSeriesWorkout useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(
            new ChangeSeriesWorkoutCommand(seriesId, workoutId, request.Name, Prescription(request.Exercises)),
            cancellationToken));

    private static async Task<IResult> RemoveWorkoutAsync(
        Guid seriesId,
        Guid workoutId,
        IRemoveSeriesWorkout useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new RemoveSeriesWorkoutCommand(seriesId, workoutId), cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> AssignAsync(
        Guid seriesId,
        AssignWorkoutSeriesRequest request,
        IAssignWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new AssignWorkoutSeriesCommand(seriesId, request.UserId),
            cancellationToken);

        return result.Created
            ? Results.Created($"/workout-series/{result.Series.Id}", result.Series)
            : Results.Ok(result.Series);
    }

    /// <summary>
    /// Nothing assigned is answered with 204: the athlete has no series, and an empty body says that
    /// without inventing a payload the client would have to null-check.
    /// </summary>
    private static async Task<IResult> GetMySeriesAsync(
        IGetMyWorkoutSeries useCase,
        CancellationToken cancellationToken) =>
        await useCase.ExecuteAsync(cancellationToken) is { } mine
            ? Results.Ok(mine)
            : Results.NoContent();

    private static IReadOnlyList<PrescribedExerciseInput> Prescription(
        IReadOnlyList<PrescribedExerciseRequest> exercises) =>
        [.. exercises.Select(exercise => new PrescribedExerciseInput(
            exercise.ExerciseId,
            exercise.Sets,
            exercise.Repetitions,
            exercise.LoadKilograms))];
}

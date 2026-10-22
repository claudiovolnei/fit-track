using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the planning slice. The series side keeps the personal trainer policy; the
/// athlete side is the roster of both profiles, and each use case re-states who may act and whose
/// athletes the caller may touch.
/// </summary>
internal static class WorkoutSeriesEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var athletes = app.MapGroup("/athletes")
            .WithTags("Athletes")
            .RequireAuthorization(Policies.TrainerOrAdmin);

        athletes.MapGet("/", ListAthletesAsync);
        athletes.MapGet("/{userId:guid}/workouts", ListAthleteWorkoutsAsync);
        athletes.MapPost("/{userId:guid}/trainer", LinkTrainerAsync);
        athletes.MapDelete("/{userId:guid}/trainer", UnlinkTrainerAsync);

        // The cycle of a series belongs to the personal trainer: the administrator links people, the
        // trainer decides the plan. Its own group, so the policy says that without touching the
        // roster's.
        var cycles = app.MapGroup("/athletes/{athleteId:guid}/series")
            .WithTags("Workout series")
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        cycles.MapPost("/end", EndSeriesAsync);

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


        // The athlete reads their own series through a singular route: no id, no way to ask for a
        // series that is not theirs.
        app.MapGet("/workout-series/mine", GetMySeriesAsync)
            .WithTags("Workout series")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListAthletesAsync(
        string? search,
        int? page,
        int? pageSize,
        bool? candidates,
        IListAthletes useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await useCase.ExecuteAsync(query, candidates == true, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

    private static async Task<IResult> ListAthleteWorkoutsAsync(
        Guid userId,
        string? search,
        int? page,
        int? pageSize,
        IListAthleteWorkouts useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await useCase.ExecuteAsync(new ListAthleteWorkoutsCommand(userId), query, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

    private static async Task<IResult> LinkTrainerAsync(
        Guid userId,
        LinkTrainerRequest request,
        ILinkAthlete useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new LinkAthleteCommand(userId, request.PersonalTrainerId), cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkTrainerAsync(
        Guid userId,
        IUnlinkAthlete useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new UnlinkAthleteCommand(userId), cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ListSeriesAsync(
        string? search,
        int? page,
        int? pageSize,
        IListWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await useCase.ExecuteAsync(query, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

    private static async Task<IResult> CreateSeriesAsync(
        CreateWorkoutSeriesRequest request,
        ICreateWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        var series = await useCase.ExecuteAsync(
            new CreateWorkoutSeriesCommand(request.Name, request.AthleteId, request.CopyPreviousWorkouts),
            cancellationToken);

        return Results.Created($"/workout-series/{series.Id}", series);
    }

    /// <summary>
    /// Ends the cycle the athlete is in: the athlete is left without a series, so the trainer can
    /// create the next one when the time comes.
    /// </summary>
    private static async Task<IResult> EndSeriesAsync(
        Guid athleteId,
        IEndWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new EndWorkoutSeriesCommand(athleteId), cancellationToken);

        return Results.NoContent();
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
            exercise.LoadKilograms,
            exercise.RestSeconds))];
}

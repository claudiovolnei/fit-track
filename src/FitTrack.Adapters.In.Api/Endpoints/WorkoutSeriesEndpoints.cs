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
         athletes.MapDelete("/{userId:guid}/sessions/{sessionId:guid}", CancelAthleteWorkoutAsync);

        // The cycle of a series belongs to the personal trainer — and to the athlete the
        // administrator released to build their own plan. The group is open to any authenticated
        // account and each use case states who may act, the same way the roster does.
        var cycles = app.MapGroup("/athletes/{athleteId:guid}/series")
            .WithTags("Workout series")
            .RequireAuthorization();

        cycles.MapPost("/end", EndSeriesAsync);

        // The series side is the personal trainer's — and the released athlete's. Who may act is
        // stated by the use cases, which read the release from the account.
        var series = app.MapGroup("/workout-series")
            .WithTags("Workout series")
            .RequireAuthorization();

        series.MapGet("/", ListSeriesAsync);
        series.MapPost("/", CreateSeriesAsync);
        series.MapGet("/{seriesId:guid}", GetSeriesAsync);
        series.MapPut("/{seriesId:guid}", RenameSeriesAsync);
        series.MapPost("/{seriesId:guid}/workouts", AddWorkoutAsync);
        series.MapPut("/{seriesId:guid}/workouts/{workoutId:guid}", ChangeWorkoutAsync);
        series.MapDelete("/{seriesId:guid}/workouts/{workoutId:guid}", RemoveWorkoutAsync);

        // Assigning an existing series and listing its followers are the author's verbs: the group
        // is open to any authenticated account and the use cases state who may act.
        series.MapPost("/{seriesId:guid}/athletes", AssignSeriesAsync);
        series.MapGet("/{seriesId:guid}/athletes", ListSeriesFollowersAsync);

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
            // The single athlete leads the merged list: with only it the destination order — and so
            // the sheet a copy starts from — is exactly the one the singular flow had.
            new CreateWorkoutSeriesCommand(
                request.Name,
                request.AthleteId,
                request.CopyPreviousWorkouts,
                request.AthleteIds),
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

    /// <summary>
    /// Points one or more athletes at a series that already exists. There is nothing to return — the
    /// series stays as it is and the assignments speak by themselves, so 204 it is.
    /// </summary>
    private static async Task<IResult> AssignSeriesAsync(
        Guid seriesId,
        AssignSeriesRequest request,
        IAssignWorkoutSeries useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(
            // The same merge as the creation: with only the single id the destination order is
            // exactly the one the flows that predate the plural rely on.
            new AssignWorkoutSeriesCommand(seriesId, request.AthleteId, request.AthleteIds),
            cancellationToken);

        return Results.NoContent();
    }

    /// <summary>
    /// The athletes following the series, empty list when nobody does: the caller asked the state of
    /// the roster, and an empty answer is a list, not an absence.
    /// </summary>
    private static async Task<IResult> ListSeriesFollowersAsync(
        Guid seriesId,
        IListWorkoutSeriesFollowers useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(seriesId, cancellationToken));

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

     private static async Task<IResult> CancelAthleteWorkoutAsync(
         Guid userId,
         Guid sessionId,
         ICancelAthleteWorkout useCase,
         CancellationToken cancellationToken)
     {
         await useCase.ExecuteAsync(new CancelAthleteWorkoutCommand(userId, sessionId), cancellationToken);

         return Results.NoContent();
     }
}

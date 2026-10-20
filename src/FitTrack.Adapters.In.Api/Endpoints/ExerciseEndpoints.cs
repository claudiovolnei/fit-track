using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>Inbound adapter for the exercise catalog. Mutations require the personal trainer policy.</summary>
internal static class ExerciseEndpoints
{
    public static IEndpointRouteBuilder MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/exercises")
            .WithTags("Exercise catalog")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        return app;
    }

    private static async Task<IResult> ListAsync(
        string? search,
        int? page,
        int? pageSize,
        string? muscleGroupId,
        IListExercises useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        // The catalog sends an empty string when "todos os grupos" is selected; it means no filter.
        var muscleGroup = Guid.TryParse(muscleGroupId, out var parsed) ? parsed : (Guid?)null;
        var result = await useCase.ExecuteAsync(query, muscleGroup, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

    private static async Task<IResult> CreateAsync(
        CreateExerciseRequest request,
        ICreateExercise useCase,
        CancellationToken cancellationToken)
    {
        var exercise = await useCase.ExecuteAsync(
            new CreateExerciseCommand(request.Name, request.MuscleGroupId),
            cancellationToken);

        return Results.Created($"/exercises/{exercise.Id}", exercise);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateExerciseRequest request,
        IUpdateExercise useCase,
        CancellationToken cancellationToken)
    {
        var exercise = await useCase.ExecuteAsync(
            new UpdateExerciseCommand(id, request.Name, request.MuscleGroupId, request.IsActive),
            cancellationToken);

        return Results.Ok(exercise);
    }
}

using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>Inbound adapter for the muscle group catalog. Mutations require the personal trainer policy.</summary>
internal static class MuscleGroupEndpoints
{
    public static IEndpointRouteBuilder MapMuscleGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/muscle-groups")
            .WithTags("Muscle group catalog")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        return app;
    }

    private static async Task<IResult> ListAsync(IListMuscleGroups useCase, CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        CreateMuscleGroupRequest request,
        ICreateMuscleGroup useCase,
        CancellationToken cancellationToken)
    {
        var muscleGroup = await useCase.ExecuteAsync(
            new CreateMuscleGroupCommand(request.Name),
            cancellationToken);

        return Results.Created($"/muscle-groups/{muscleGroup.Id}", muscleGroup);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateMuscleGroupRequest request,
        IUpdateMuscleGroup useCase,
        CancellationToken cancellationToken)
    {
        var muscleGroup = await useCase.ExecuteAsync(
            new UpdateMuscleGroupCommand(id, request.Name),
            cancellationToken);

        return Results.Ok(muscleGroup);
    }
}

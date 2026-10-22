using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for account administration. The list is a read of its own — a use case that states
/// who may read it plus a query port; writing an account is one store call whose input is validated
/// here, which is why creation and the role changes drive Identity from this adapter.
/// </summary>
internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Administration")
            .RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/users", ListAsync);        group.MapPost("/users", CreateAsync);
        group.MapPost("/users/{userId:guid}/roles", GrantAsync);
        group.MapDelete("/users/{userId:guid}/roles/{role}", RevokeAsync);
        group.MapPost("/users/{userId:guid}/password", ResetPasswordAsync);
        group.MapPost("/users/{userId:guid}/self-authoring", GrantSelfAuthoringAsync);
        group.MapDelete("/users/{userId:guid}/self-authoring", RevokeSelfAuthoringAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(
        string? search,
        string? role,
        int? page,
        int? pageSize,
        IListUsers useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        // An unknown role is refused the same way a grant refuses it, so the filter cannot
        // silently answer an empty list for a typo.
        var known = string.IsNullOrWhiteSpace(role) ? null : KnownRole(role);

        var result = await useCase.ExecuteAsync(query, known, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        if (AccountProblems.ProblemWithName(request.Name) is { } nameProblem)
        {
            return nameProblem;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return AccountProblems.Invalid("Email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return AccountProblems.Invalid("Password", "Password is required.");
        }

        var roles = KnownRoles(request.Roles);

        var account = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            UserName = request.Email,
            Email = request.Email,
        };

        var created = await users.CreateAsync(account, request.Password);
        if (!created.Succeeded)
        {
            return AccountProblems.Validation(created);
        }

        foreach (var role in roles)
        {
            await users.AddToRoleAsync(account, role);
        }

        return Results.Created(
            $"/admin/users/{account.Id}",
            new UserAccountDto(account.Id, account.Name, account.Email!, roles));
    }

    private static async Task<IResult> GrantAsync(
        Guid userId,
        GrantRoleRequest request,
        UserManager<AppUser> users)
    {
        var account = await FindAsync(users, userId);
        var role = KnownRole(request.Role);

        return await users.IsInRoleAsync(account, role)
            ? Results.NoContent()
            : AccountProblems.Store(await users.AddToRoleAsync(account, role));
    }

    private static async Task<IResult> RevokeAsync(
        Guid userId,
        string role,
        UserManager<AppUser> users)
    {
        var account = await FindAsync(users, userId);
        var known = KnownRole(role);

        return await users.IsInRoleAsync(account, known)
            ? AccountProblems.Store(await users.RemoveFromRoleAsync(account, known))
            : Results.NoContent();
    }

    /// <summary>
    /// The profiles the new account takes, in the order the app presents them. Each one goes through
    /// <see cref="KnownRole"/>, so an unknown profile is refused the same way a grant refuses it; an
    /// empty choice is refused too, because an account with no profile signs in and sees nothing.
    /// </summary>
    private static IReadOnlyList<string> KnownRoles(IReadOnlyList<string>? requested)
    {
        if (requested is null || requested.Count == 0)
        {
            throw new DomainException(
                "role.required",
                $"Choose at least one role. Known roles: {string.Join(", ", Roles.All)}.");
        }

        var known = requested.Select(KnownRole).ToHashSet(StringComparer.Ordinal);

        return [.. Roles.All.Where(known.Contains)];
    }

    private static async Task<IResult> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequest request,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return AccountProblems.Invalid("Password", "Password is required.");
        }

        var account = await FindAsync(users, userId);

        var token = await users.GeneratePasswordResetTokenAsync(account);
        var result = await users.ResetPasswordAsync(account, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return AccountProblems.Validation(result);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Releases the account to build its own training plan, without a personal trainer. The release
    /// lives in the account, so it takes effect at once — a revocation does not wait for the next
    /// sign-in.
    /// </summary>
    private static async Task<IResult> GrantSelfAuthoringAsync(Guid userId, UserManager<AppUser> users)
    {
        var account = await FindAsync(users, userId);

        if (account.CanAuthorOwnWorkout)
        {
            return Results.NoContent();
        }

        account.CanAuthorOwnWorkout = true;

        return AccountProblems.Store(await users.UpdateAsync(account));
    }

    /// <summary>Takes the release back: the athlete needs a personal trainer again.</summary>
    private static async Task<IResult> RevokeSelfAuthoringAsync(Guid userId, UserManager<AppUser> users)
    {
        var account = await FindAsync(users, userId);

        if (!account.CanAuthorOwnWorkout)
        {
            return Results.NoContent();
        }

        account.CanAuthorOwnWorkout = false;

        return AccountProblems.Store(await users.UpdateAsync(account));
    }

    private static async Task<AppUser> FindAsync(UserManager<AppUser> users, Guid userId) =>
        await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"No account with id '{userId}'.");

    private static string KnownRole(string role) =>
        Roles.All.FirstOrDefault(known => string.Equals(known, role, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                "role.unknown",
                $"Unknown role '{role}'. Known roles: {string.Join(", ", Roles.All)}.");
}

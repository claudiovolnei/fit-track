using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Application.Exceptions;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for account administration. Roles live in Identity, which the inbound
/// adapters already drive for accounts (see <see cref="AuthEndpoints"/>), so a grant is one
/// store call plus the input check, not a use case of its own.
/// </summary>
internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Administration")
            .RequireAuthorization(Policies.AdminOnly);

        group.MapPost("/users/{userId:guid}/roles", GrantAsync);
        group.MapDelete("/users/{userId:guid}/roles/{role}", RevokeAsync);

        return app;
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
            : Store(await users.AddToRoleAsync(account, role));
    }

    private static async Task<IResult> RevokeAsync(
        Guid userId,
        string role,
        UserManager<AppUser> users)
    {
        var account = await FindAsync(users, userId);
        var known = KnownRole(role);

        return await users.IsInRoleAsync(account, known)
            ? Store(await users.RemoveFromRoleAsync(account, known))
            : Results.NoContent();
    }

    private static async Task<AppUser> FindAsync(UserManager<AppUser> users, Guid userId) =>
        await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"No account with id '{userId}'.");

    private static string KnownRole(string role) =>
        Roles.All.FirstOrDefault(known => string.Equals(known, role, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                "role.unknown",
                $"Unknown role '{role}'. Known roles: {string.Join(", ", Roles.All)}.");

    private static IResult Store(IdentityResult result) =>
        result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
}

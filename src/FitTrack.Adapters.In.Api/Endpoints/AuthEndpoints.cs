using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Adapters.In.Api.Seeding;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>Inbound adapter for account registration and sign-in.</summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterAccountRequest request,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        if (AccountProblems.ProblemWithName(request.Name) is { } nameProblem)
        {
            return nameProblem;
        }

        var account = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            UserName = request.Email,
            Email = request.Email,
        };

        var result = await users.CreateAsync(account, request.Password);
        if (!result.Succeeded)
        {
            return AccountProblems.Validation(result);
        }

        await users.AddToRoleAsync(account, Roles.User);

        return Results.Created($"/accounts/{account.Id}", new { account.Id, account.Name, account.Email });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> users,
        JwtTokenIssuer tokenIssuer,
        CancellationToken cancellationToken)
    {
        var account = await users.FindByEmailAsync(request.Email);
        if (account is null || !await users.CheckPasswordAsync(account, request.Password))
        {
            return Results.Problem(
                title: "Invalid credentials.",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var roles = await users.GetRolesAsync(account);

        return Results.Ok(tokenIssuer.Issue(account.Id, account.Email ?? account.UserName ?? string.Empty, roles));
    }
}

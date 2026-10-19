using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the account avatar. The bytes live in the Identity store, so reads and writes
/// go through <see cref="UserManager{TUser}"/>; the application layer only sees the current user.
/// </summary>
internal static class AvatarEndpoints
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    public static IEndpointRouteBuilder MapAvatarEndpoints(this IEndpointRouteBuilder app)
    {
        var authed = app.MapGroup("/").WithTags("Account").RequireAuthorization();

        authed.MapPost("/me/avatar", UploadAsync).DisableAntiforgery();
        authed.MapDelete("/me/avatar", DeleteAsync);
        authed.MapGet("/me/avatar", ReadMineAsync);
        authed.MapGet("/users/{userId:guid}/avatar", ReadAsync);

        return app;
    }

    private static async Task<IResult> UploadAsync(
        IFormFile avatar,
        ICurrentUser currentUser,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        if (avatar is null || avatar.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Avatar"] = ["Choose an image."],
            });
        }

        if (avatar.Length > MaxBytes)
        {
            return Results.Problem(
                title: "avatar.too_large",
                detail: "The image must be at most 2 MB.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!AllowedTypes.Contains(avatar.ContentType ?? string.Empty))
        {
            return Results.Problem(
                title: "avatar.type.invalid",
                detail: "Only JPEG, PNG and WebP images are accepted.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var account = await users.FindByIdAsync(currentUser.Id.Value.ToString());
        if (account is null)
        {
            throw new NotFoundException($"No account with id '{currentUser.Id.Value}'.");
        }

        using var stream = avatar.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        account.AvatarBytes = memory.ToArray();
        account.AvatarContentType = avatar.ContentType;

        var result = await users.UpdateAsync(account);
        if (!result.Succeeded)
        {
            return AccountProblems.Validation(result);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        ICurrentUser currentUser,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(currentUser.Id.Value.ToString());
        if (account is null)
        {
            throw new NotFoundException($"No account with id '{currentUser.Id.Value}'.");
        }

        account.AvatarBytes = null;
        account.AvatarContentType = null;

        await users.UpdateAsync(account);

        return Results.NoContent();
    }

    private static async Task<IResult> ReadMineAsync(
        ICurrentUser currentUser,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(currentUser.Id.Value.ToString());

        return AvatarResult(account);
    }

    private static async Task<IResult> ReadAsync(
        Guid userId,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(userId.ToString());

        return AvatarResult(account);
    }

    private static IResult AvatarResult(AppUser? account)
    {
        if (account?.AvatarBytes is not { Length: > 0 } bytes || string.IsNullOrWhiteSpace(account.AvatarContentType))
        {
            return Results.NotFound();
        }

        return Results.File(bytes, account.AvatarContentType);
    }
}

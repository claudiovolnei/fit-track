using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.Out.Persistence.Accounts;

/// <summary>
/// Identity account for the fit-track application. The Identity context owns login
/// and profile; workout aggregates only ever hold the matching <c>UserId</c>.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>
    /// Display name of the account. The athlete reads the name of the trainer that authored his series;
    /// while it is empty the read models fall back to the e-mail, which is what accounts created before
    /// this column existed get.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image bytes, stored in the database so a single source of truth survives across
    /// containers without a shared volume. Null when the account has no photo.
    /// </summary>
    public byte[]? AvatarBytes { get; set; }

    /// <summary>MIME type of <see cref="AvatarBytes"/> (e.g. image/jpeg), or null when there is no photo.</summary>
    public string? AvatarContentType { get; set; }

    /// <summary>
    /// Whether the administrator released this account to build its own training plan, without a
    /// personal trainer. The release lives in the account and not in the token, so a revocation
    /// takes effect at once.
    /// </summary>
    public bool CanAuthorOwnWorkout { get; set; }
}

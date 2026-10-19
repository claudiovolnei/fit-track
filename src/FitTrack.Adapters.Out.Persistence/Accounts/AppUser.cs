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
}

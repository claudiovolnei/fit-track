using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.Out.Persistence.Accounts;

/// <summary>
/// Identity account for the fit-track application. The Identity context owns login
/// and profile; workout aggregates only ever hold the matching <c>UserId</c>.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>;

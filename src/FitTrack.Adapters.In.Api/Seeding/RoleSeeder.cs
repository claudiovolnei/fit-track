using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.In.Api.Seeding;

/// <summary>
/// Creates the application roles. Registration assigns <see cref="Roles.User"/> to the first
/// account, so this must run in every environment, not only Development.
/// </summary>
internal static class RoleSeeder
{
    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}

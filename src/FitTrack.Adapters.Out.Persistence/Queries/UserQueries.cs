using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the accounts the administrator manages. The profiles live in the Identity
/// tables, so the query reads the accounts and the user-role links and groups the roles in memory —
/// the same shape <see cref="AthleteQueries"/> uses to answer over Identity.
/// </summary>
internal sealed class UserQueries(FitTrackDbContext dbContext) : IQueryUsers
{
    public async Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Name, user.Email })
            .ToListAsync(cancellationToken);

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Join(
                dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, RoleName = role.Name! })
            .ToListAsync(cancellationToken);

        var byUser = roles
            .GroupBy(entry => entry.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.RoleName).OrderBy(Rank).ToArray());

        return
        [
            .. accounts.Select(account => new UserAccountDto(
                account.Id,
                DisplayName(account.Name, account.Email),
                account.Email ?? string.Empty,
                byUser.TryGetValue(account.Id, out var assigned) ? assigned : [])),
        ];
    }

    /// <summary>
    /// Who the account is. The e-mail stands in while the display name is empty, so the administrator
    /// screen never shows a blank line for accounts created before the name column existed.
    /// </summary>
    private static string DisplayName(string? name, string? email) =>
        !string.IsNullOrWhiteSpace(name) ? name : email ?? string.Empty;

    /// <summary>The profiles in the order the app presents them; one the app does not know goes last.</summary>
    private static int Rank(string role)
    {
        for (var index = 0; index < Roles.All.Count; index++)
        {
            if (string.Equals(Roles.All[index], role, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return Roles.All.Count;
    }
}

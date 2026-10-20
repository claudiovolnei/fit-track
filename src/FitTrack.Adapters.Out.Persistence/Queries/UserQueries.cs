using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the accounts the administrator manages. The profiles live in the Identity
/// tables, so the query reads the accounts and the user-role links and groups the roles in memory -
/// the same shape <see cref="AthleteQueries"/> uses to answer over Identity.
/// </summary>
internal sealed class UserQueries(FitTrackDbContext dbContext) : IQueryUsers
{
    public async Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken) =>
        (await ListAsync(ListQuery.All, cancellationToken)).Items;

    public async Task<PagedResult<UserAccountDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var source = dbContext.Users.AsNoTracking();

        if (query.Search is { } search)
        {
            var pattern = SearchPattern.Contains(search);
            var escape = "\\";

            source = source.Where(account =>
                EF.Functions.ILike(account.Name!, pattern, escape)
                || EF.Functions.ILike(account.Email!, pattern, escape));
        }

        var total = await source.CountAsync(cancellationToken);

        var ordered = source
            .OrderBy(account => account.Email)
            .ThenBy(account => account.Id);

        var page = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var accounts = await page
            .Select(account => new { account.Id, account.Name, account.Email, account.AvatarBytes })
            .ToListAsync(cancellationToken);

        var ids = accounts.Select(account => account.Id).ToList();

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => ids.Contains(userRole.UserId))
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

        var items = accounts
            .Select(account => new UserAccountDto(
                account.Id,
                DisplayName(account.Name, account.Email),
                account.Email ?? string.Empty,
                byUser.TryGetValue(account.Id, out var assigned) ? assigned : [],
                account.AvatarBytes is { Length: > 0 }))
            .ToList();

        return new PagedResult<UserAccountDto>(items, query.Page, query.PageSize, total);
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

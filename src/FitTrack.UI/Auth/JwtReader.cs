using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace FitTrack.UI.Auth;

/// <summary>
/// Reads the claims the API put in the access token. The UI only decodes the payload to
/// build the client-side identity; the API remains the only authority that validates it.
/// </summary>
public static class JwtReader
{
    /// <summary>
    /// The API issues the role under the .NET claim type, while the OpenID Connect convention
    /// (and any other client) uses the short name; both are accepted here.
    /// </summary>
    private static readonly string[] RoleClaimNames = [ClaimTypes.Role, "role"];

    public static IReadOnlyList<Claim> ReadClaims(string token)
    {
        var segments = token.Split('.');
        if (segments.Length < 2)
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(DecodeBase64Url(segments[1]));
            var root = document.RootElement;
            var claims = new List<Claim>();

            AddIfPresent(claims, root, "sub", ClaimTypes.NameIdentifier);

            // The name is the account name the API issues; older tokens only carry the e-mail.
            if (TryGetClaimValue(root, "name") is { ValueKind: JsonValueKind.String } name
                && name.GetString()!.Length > 0)
            {
                claims.Add(new Claim(ClaimTypes.Name, name.GetString()!));
            }
            else
            {
                AddIfPresent(claims, root, "email", ClaimTypes.Name);
            }

            if (TryGetRoles(root, out var roles))
            {
                if (roles.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(roles.EnumerateArray()
                        .Where(role => role.ValueKind == JsonValueKind.String)
                        .Select(role => new Claim(ClaimTypes.Role, role.GetString()!)));
                }
                else if (roles.ValueKind == JsonValueKind.String)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.GetString()!));
                }
            }

            return claims;
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return [];
        }
    }

    private static bool TryGetRoles(JsonElement root, out JsonElement roles)
    {
        foreach (var name in RoleClaimNames)
        {
            if (root.TryGetProperty(name, out roles))
            {
                return true;
            }
        }

        roles = default;
        return false;
    }

    private static void AddIfPresent(List<Claim> claims, JsonElement root, string name, string claimType)
    {
        if (TryGetClaimValue(root, name) is { ValueKind: JsonValueKind.String } value)
        {
            claims.Add(new Claim(claimType, value.GetString()!));
        }
    }

    private static JsonElement TryGetClaimValue(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) ? value : default;
    }

    /// <summary>
    /// Whether the access token has expired according to its <c>exp</c> claim (UTC seconds).
    /// A token without a valid <c>exp</c> is treated as expired, so a malformed token never
    /// keeps the user authenticated.
    /// </summary>
    public static bool IsExpired(string token, DateTimeOffset now)
    {
        var expiration = GetExpiration(token);
        return expiration is null || expiration.Value <= now;
    }

    /// <summary>Reads the <c>exp</c> claim as a UTC timestamp, or null when absent/invalid.</summary>
    public static DateTimeOffset? GetExpiration(string token)
    {
        var segments = token.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(DecodeBase64Url(segments[1]));
            if (!document.RootElement.TryGetProperty("exp", out var exp))
            {
                return null;
            }

            if (exp.ValueKind == JsonValueKind.Number && exp.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }

            if (exp.ValueKind == JsonValueKind.String && long.TryParse(exp.GetString(), out var parsed))
            {
                return DateTimeOffset.FromUnixTimeSeconds(parsed);
            }

            return null;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentOutOfRangeException or OverflowException)
        {
            return null;
        }
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };

        return Convert.FromBase64String(padded);
    }
}

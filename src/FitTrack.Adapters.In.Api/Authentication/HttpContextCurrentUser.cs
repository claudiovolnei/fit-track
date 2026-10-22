using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace FitTrack.Adapters.In.Api.Authentication;

/// <summary>
/// Reads the authenticated caller from the HTTP context. This is an inbound-adapter
/// concern, which is why it lives here instead of in the application layer.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public UserId Id => new(ReadUserId());

    public bool IsPersonalTrainer => accessor.HttpContext?.User.IsInRole(Roles.PersonalTrainer) ?? false;

    public bool IsAdmin => accessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;

    public bool IsAthlete => accessor.HttpContext?.User.IsInRole(Roles.User) ?? false;

    private Guid ReadUserId()
    {
        var subject = accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}

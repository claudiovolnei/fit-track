using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Exposes the authenticated caller to the application layer, so authorization
/// lives in the use cases instead of only on the HTTP endpoint.
/// </summary>
public interface ICurrentUser
{
    UserId Id { get; }

    /// <summary>Authoring the catalog and the training plans belongs to the personal trainer.</summary>
    bool IsPersonalTrainer { get; }
}

namespace FitTrack.Domain.Common;

/// <summary>
/// Signals that a domain invariant was violated. Carries a stable <see cref="Code"/>
/// so adapters can translate it without matching on the message text.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

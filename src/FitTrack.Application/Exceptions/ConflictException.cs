namespace FitTrack.Application.Exceptions;

/// <summary>
/// Raised when a use case refuses an intention because the target aggregate is referenced
/// elsewhere (409). Carries a stable <see cref="Code"/> so adapters can translate it without
/// matching on the message text.
/// </summary>
public sealed class ConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

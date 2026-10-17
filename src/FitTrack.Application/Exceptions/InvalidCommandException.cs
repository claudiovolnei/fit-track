namespace FitTrack.Application.Exceptions;

/// <summary>
/// Raised when a command breaks a precondition that needs an out port (such as the
/// clock) and therefore cannot live on the aggregate as a pure invariant.
/// </summary>
public sealed class InvalidCommandException(string message) : Exception(message);

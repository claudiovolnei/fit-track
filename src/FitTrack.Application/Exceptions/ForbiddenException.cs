namespace FitTrack.Application.Exceptions;

/// <summary>Raised when the current user is not allowed to perform the requested intention.</summary>
public sealed class ForbiddenException(string message) : Exception(message);

namespace FitTrack.Application.Exceptions;

/// <summary>Raised when a use case targets an aggregate that does not exist.</summary>
public sealed class NotFoundException(string message) : Exception(message);

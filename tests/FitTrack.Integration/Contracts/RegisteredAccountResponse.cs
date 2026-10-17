namespace FitTrack.Integration.Contracts;

/// <summary>Account the API created for a registration.</summary>
public sealed record RegisteredAccountResponse(Guid Id, string Email);

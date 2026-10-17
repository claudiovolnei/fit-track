namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for creating an account.</summary>
internal sealed record RegisterAccountRequest(string Email, string Password);

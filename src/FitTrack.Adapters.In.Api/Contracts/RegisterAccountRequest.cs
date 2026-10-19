namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for creating an account. The name is what the athlete sees on the trainer.</summary>
internal sealed record RegisterAccountRequest(string? Name, string Email, string Password);

namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>
/// Wire contract: the administrator creates an account and hands it its profiles. The body is the
/// same shape self-registration uses plus the roles, which only the administrator may choose.
/// </summary>
internal sealed record CreateUserRequest(
    string? Name,
    string? Email,
    string? Password,
    IReadOnlyList<string>? Roles);

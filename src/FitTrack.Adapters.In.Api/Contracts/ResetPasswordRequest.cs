namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>The new password an administrator sets for an account.</summary>
internal sealed record ResetPasswordRequest(string NewPassword);

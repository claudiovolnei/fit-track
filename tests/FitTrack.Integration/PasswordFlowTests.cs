using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// Password change and reset, end to end: a user changes their own password (proving the current one),
/// and an administrator redefines one without knowing it. The two refuse the same way the Identity
/// store does, and the authorization boundary holds.
/// </summary>
public sealed class PasswordFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task ChangePassword_WithCorrectCurrent_Succeeds()
    {
        var password = "SenhaInicial1!";
        var email = $"pwd-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/auth/change-password",
            new ChangePasswordRequest(password, "NovaSenha2!"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var oldLogin = await factory.CreateClient().PostAsJsonAsync(
            "/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await factory.CreateClient().PostAsJsonAsync(
            "/auth/login", new { email, password = "NovaSenha2!" });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task ChangePassword_WithWrongCurrent_Returns400()
    {
        var password = "SenhaInicial1!";
        var email = $"pwd-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/auth/change-password",
            new ChangePasswordRequest("senha-errada", "NovaSenha2!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem!.Errors);
        Assert.Contains("PasswordMismatch", problem.Errors!.Keys);
    }

    [RequiresPostgresFact]
    public async Task ChangePassword_WithWeakNew_Returns400()
    {
        var password = "SenhaInicial1!";
        var email = $"pwd-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/auth/change-password",
            new ChangePasswordRequest(password, "curta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem!.Errors);
        Assert.True(problem.Errors!.ContainsKey("PasswordTooShort")
            || problem.Errors!.ContainsKey("PasswordRequiresDigit")
            || problem.Errors!.ContainsKey("PasswordRequiresUpper")
            || problem.Errors!.ContainsKey("PasswordRequiresLower"));
    }

    [RequiresPostgresFact]
    public async Task ChangePassword_WithSamePassword_Returns400()
    {
        var password = "SenhaInicial1!";
        var email = $"pwd-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/auth/change-password",
            new ChangePasswordRequest(password, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("password.same", problem!.Title);
    }

    [RequiresPostgresFact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/auth/change-password",
            new ChangePasswordRequest("a", "b"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task ResetPassword_AsAdmin_Succeeds()
    {
        var email = $"reset-{Guid.NewGuid():N}@fittrack.local";
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);

        var created = await admin.PostAsJsonAsync(
            "/admin/users",
            new { name = "Conta Reset", email, password = "SenhaInicial1!", roles = new[] { "User" } });
        await created.EnsureSuccessWithBodyAsync();
        var account = await created.Content.ReadFromJsonAsync<UserAccountResponse>();

        var reset = await admin.PostAsJsonAsync(
            $"/admin/users/{account!.UserId}/password",
            new ResetPasswordRequest("NovaSenha2!"));

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var login = await factory.CreateClient().PostAsJsonAsync(
            "/auth/login", new { email, password = "NovaSenha2!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task ResetPassword_AsNonAdmin_Returns403()
    {
        var email = $"reset-{Guid.NewGuid():N}@fittrack.local";
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);
        var created = await admin.PostAsJsonAsync(
            "/admin/users",
            new { name = "Conta Reset", email, password = "SenhaInicial1!", roles = new[] { "User" } });
        await created.EnsureSuccessWithBodyAsync();
        var account = await created.Content.ReadFromJsonAsync<UserAccountResponse>();

        var seededAthlete = await factory.CreateClient().SignInAsync("athlete@fittrack.local", "Athlete123$");
        var response = await seededAthlete.PostAsJsonAsync(
            $"/admin/users/{account!.UserId}/password",
            new ResetPasswordRequest("NovaSenha2!"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// Avatar upload and read, end to end: a valid image is stored and read back byte for byte, the
/// validations refuse the bad ones with stable codes, and the authorization boundary holds.
/// </summary>
public sealed class AvatarFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    [RequiresPostgresFact]
    public async Task Upload_ValidPng_ThenRead_ReturnsSameBytes()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "tiny.png");

        var upload = await client.PostAsync("/me/avatar", content);
        Assert.Equal(HttpStatusCode.NoContent, upload.StatusCode);

        var read = await client.GetAsync("/me/avatar");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal("image/png", read.Content.Headers.ContentType?.MediaType);
        var bytes = await read.Content.ReadAsByteArrayAsync();
        Assert.Equal(TinyPng, bytes);
    }

    [RequiresPostgresFact]
    public async Task Upload_InvalidType_Returns400()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not an image"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "avatar", "note.txt");

        var response = await client.PostAsync("/me/avatar", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("avatar.type.invalid", problem!.Title);
    }

    [RequiresPostgresFact]
    public async Task Upload_TooLarge_Returns400()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var big = new byte[2 * 1024 * 1024 + 1024];
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(big);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "avatar", "big.jpg");

        var response = await client.PostAsync("/me/avatar", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("avatar.too_large", problem!.Title);
    }

    [RequiresPostgresFact]
    public async Task Read_WithoutUpload_Returns404()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        var response = await client.GetAsync("/me/avatar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Delete_AfterUpload_ReadReturns404()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var client = factory.CreateClient();
        await client.RegisterAccountAsync(email, password);
        await client.SignInAsync(email, password);

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(TinyPng);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "avatar", "tiny.png");
            await client.PostAsync("/me/avatar", content);
        }

        var delete = await client.DeleteAsync("/me/avatar");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var read = await client.GetAsync("/me/avatar");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Upload_WithoutToken_Returns401()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "tiny.png");

        var response = await factory.CreateClient().PostAsync("/me/avatar", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task ReadOtherUserAvatar_AsAuthenticated_Works()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var owner = factory.CreateClient();
        var ownerId = await owner.RegisterAccountAsync(email, password);
        await owner.SignInAsync(email, password);

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(TinyPng);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "avatar", "tiny.png");
            await owner.PostAsync("/me/avatar", content);
        }

        var reader = await factory.CreateClient().SignInAsync("athlete@fittrack.local", "Athlete123$");
        var response = await reader.GetAsync($"/users/{ownerId}/avatar");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(TinyPng, bytes);
    }

    [RequiresPostgresFact]
    public async Task AdminList_ReportsHasAvatar()
    {
        var email = $"av-{Guid.NewGuid():N}@fittrack.local";
        var password = "SenhaInicial1!";
        var owner = factory.CreateClient();
        await owner.RegisterAccountAsync(email, password);
        await owner.SignInAsync(email, password);

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(TinyPng);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "avatar", "tiny.png");
            await owner.PostAsync("/me/avatar", content);
        }

        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);
        var users = await admin.GetFromJsonAsync<List<UserAccountResponse>>("/admin/users");
        var found = users!.First(u => u.Email == email);
        Assert.True(found.HasAvatar);
    }
}

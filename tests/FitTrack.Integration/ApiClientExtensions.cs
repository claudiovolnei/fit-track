using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

internal static class ApiClientExtensions
{
    /// <summary>Signs the client in and leaves the bearer token on it for later calls.</summary>
    public static async Task<HttpClient> SignInAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        return client;
    }

    /// <summary>
    /// Registers an account and returns the id the API created for it. The name is part of registering —
    /// it is what the screens show — so the tests send one that reads as a person.
    /// </summary>
    public static async Task<Guid> RegisterAccountAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new { name = "Atleta de Integração", email, password });

        await response.EnsureSuccessWithBodyAsync();

        var created = await response.Content.ReadFromJsonAsync<RegisteredAccountResponse>();

        return created!.Id;
    }

    public static async Task<HttpClient> RegisterAsync(this HttpClient client, string email, string password)
    {
        await client.RegisterAccountAsync(email, password);

        return client;
    }

    /// <summary>Creates a muscle group with a unique name, so the caller has an id to link to.</summary>
    public static async Task<MuscleGroupResponse> CreateMuscleGroupAsync(this HttpClient client, string prefix)
    {
        var response = await client.PostAsJsonAsync("/muscle-groups", new { name = $"{prefix} {Guid.NewGuid():N}" });
        await response.EnsureSuccessWithBodyAsync();

        return (await response.Content.ReadFromJsonAsync<MuscleGroupResponse>())!;
    }

    /// <summary>Creates a series, as the signed-in trainer.</summary>
    public static async Task<WorkoutSeriesResponse> CreateSeriesAsync(this HttpClient trainerClient, string name, Guid? athleteId = null)
    {
        var response = await trainerClient.PostAsJsonAsync(
            "/workout-series",
            new SetWorkoutSeriesNameRequest(name, athleteId));

        await response.EnsureSuccessWithBodyAsync();

        return (await response.Content.ReadFromJsonAsync<WorkoutSeriesResponse>())!;
    }

    /// <summary>Adds one workout, with its prescription, to a series, as the signed-in trainer.</summary>
    public static async Task<WorkoutSeriesWorkoutResponse> AddWorkoutAsync(
        this HttpClient trainerClient,
        Guid seriesId,
        string name,
        params PrescribedExerciseRequest[] prescription)
    {
        var response = await trainerClient.PostAsJsonAsync(
            $"/workout-series/{seriesId}/workouts",
            new SetSeriesWorkoutRequest(name, prescription));

        await response.EnsureSuccessWithBodyAsync();

        return (await response.Content.ReadFromJsonAsync<WorkoutSeriesWorkoutResponse>())!;
    }

    /// <summary>
    /// Reads the series assigned to the signed-in athlete. Nothing assigned is a 204, which the API uses
    /// instead of an empty payload, so it comes back as null.
    /// </summary>
    public static async Task<MyWorkoutSeriesResponse?> ReadMySeriesAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/workout-series/mine");
        response.EnsureSuccessStatusCode();

        return response.StatusCode == HttpStatusCode.NoContent
            ? null
            : await response.Content.ReadFromJsonAsync<MyWorkoutSeriesResponse>();
    }

    /// <summary>The stable code of a refusal: the API carries it in the problem title.</summary>
    public static async Task<string> ReadProblemTitleAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        return problem?.Title ?? string.Empty;
    }

    /// <summary>
    /// Fails with the response body in the message. A bare status code hides the reason the API gave,
    /// which is the one thing a failing test needs to show.
    /// </summary>
    public static async Task<HttpResponseMessage> EnsureSuccessWithBodyAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Request to {response.RequestMessage?.RequestUri} failed with {(int)response.StatusCode} " +
                $"({response.StatusCode}): {body}");
        }

        return response;
    }
}

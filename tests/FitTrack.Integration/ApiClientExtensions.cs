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

    /// <summary>Registers an account and returns the id the API created for it.</summary>
    public static async Task<Guid> RegisterAccountAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();

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
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<MuscleGroupResponse>())!;
    }

    /// <summary>Assigns a plan to the athlete, as the signed-in trainer.</summary>
    public static async Task<WorkoutPlanResponse> AssignPlanAsync(this HttpClient trainerClient, Guid athleteId)
    {
        var response = await trainerClient.PostAsJsonAsync("/workout-plans", new AssignWorkoutPlanRequest(athleteId));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<WorkoutPlanResponse>())!;
    }

    /// <summary>Adds one option to a plan, as the signed-in trainer.</summary>
    public static async Task<WorkoutPlanOptionResponse> AddPlanOptionAsync(
        this HttpClient trainerClient,
        Guid planId,
        params Guid[] muscleGroupIds)
    {
        var response = await trainerClient.PostAsJsonAsync(
            $"/workout-plans/{planId}/options",
            new SetWorkoutPlanOptionRequest(muscleGroupIds));

        await response.EnsureSuccessWithBodyAsync();

        return (await response.Content.ReadFromJsonAsync<WorkoutPlanOptionResponse>())!;
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

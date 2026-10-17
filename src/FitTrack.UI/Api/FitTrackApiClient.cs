using System.Text;
using System.Text.Json;

namespace FitTrack.UI.Api;

/// <summary>Typed client for the fit-track HTTP API. The UI never talks to the domain directly.</summary>
public sealed class FitTrackApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken = default) =>
        PostAsync("auth/register", request, cancellationToken);

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<LoginResponse>("auth/login", request, cancellationToken);

    public Task<IReadOnlyList<MuscleGroupResponse>> GetMuscleGroupsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<MuscleGroupResponse>>("muscle-groups", cancellationToken);

    public Task<MuscleGroupResponse> CreateMuscleGroupAsync(
        CreateMuscleGroupRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<MuscleGroupResponse>("muscle-groups", request, cancellationToken);

    public Task<MuscleGroupResponse> UpdateMuscleGroupAsync(
        Guid muscleGroupId,
        UpdateMuscleGroupRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<MuscleGroupResponse>($"muscle-groups/{muscleGroupId}", request, cancellationToken);

    public Task<IReadOnlyList<ExerciseResponse>> GetExercisesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ExerciseResponse>>("exercises", cancellationToken);

    public Task<ExerciseResponse> CreateExerciseAsync(
        CreateExerciseRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ExerciseResponse>("exercises", request, cancellationToken);

    public Task<ExerciseResponse> UpdateExerciseAsync(
        Guid exerciseId,
        UpdateExerciseRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<ExerciseResponse>($"exercises/{exerciseId}", request, cancellationToken);

    public Task<IReadOnlyList<WorkoutSessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutSessionResponse>>("sessions", cancellationToken);

    public Task<WorkoutSessionResponse> StartWorkoutAsync(
        StartWorkoutRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSessionResponse>("sessions", request, cancellationToken);

    public Task<WorkoutSessionResponse> LogPerformedSetAsync(
        Guid sessionId,
        LogPerformedSetRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSessionResponse>($"sessions/{sessionId}/sets", request, cancellationToken);

    public Task<WorkoutSessionResponse> FinishWorkoutAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSessionResponse>($"sessions/{sessionId}/finish", new { }, cancellationToken);

    public Task CancelWorkoutAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"sessions/{sessionId}", cancellationToken);

    public Task<WorkoutPlanResponse?> GetMyPlanAsync(CancellationToken cancellationToken = default) =>
        GetOptionalAsync<WorkoutPlanResponse>("workout-plan", cancellationToken);

    public Task<IReadOnlyList<AthleteResponse>> GetAthletesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<AthleteResponse>>("athletes", cancellationToken);

    public Task<IReadOnlyList<WorkoutSessionResponse>> GetAthleteWorkoutsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutSessionResponse>>($"athletes/{userId}/workouts", cancellationToken);

    public Task<IReadOnlyList<WorkoutPlanResponse>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutPlanResponse>>("workout-plans", cancellationToken);

    public Task<WorkoutPlanResponse> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        GetAsync<WorkoutPlanResponse>($"workout-plans/{planId}", cancellationToken);

    public Task<WorkoutPlanResponse> AssignPlanAsync(
        AssignWorkoutPlanRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutPlanResponse>("workout-plans", request, cancellationToken);

    public Task<WorkoutPlanOptionResponse> AddPlanOptionAsync(
        Guid planId,
        SetWorkoutPlanOptionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutPlanOptionResponse>($"workout-plans/{planId}/options", request, cancellationToken);

    public Task<WorkoutPlanOptionResponse> ChangePlanOptionAsync(
        Guid planId,
        Guid optionId,
        SetWorkoutPlanOptionRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<WorkoutPlanOptionResponse>($"workout-plans/{planId}/options/{optionId}", request, cancellationToken);

    public Task RemovePlanOptionAsync(
        Guid planId,
        Guid optionId,
        CancellationToken cancellationToken = default) =>
        DeleteAsync($"workout-plans/{planId}/options/{optionId}", cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        return await ReadAsync<T>(response, cancellationToken);
    }

    /// <summary>
    /// Reads a resource that may legitimately be absent. The API answers <c>200</c> with an empty body
    /// when the athlete has no plan yet, which is a state and not an error.
    /// </summary>
    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(payload) || payload.Trim() == "null"
            ? default
            : JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    private async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(path, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(path, ToJsonContent(body), cancellationToken);

        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(path, ToJsonContent(body), cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsync(path, ToJsonContent(body), cancellationToken);

        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new ApiException(response.StatusCode, "The API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ReadProblemMessage(payload) ?? response.ReasonPhrase ?? "The request failed.";

        throw new ApiException(response.StatusCode, message);
    }

    private static string? ReadProblemMessage(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in new[] { "detail", "title", "message" })
            {
                if (document.RootElement.TryGetProperty(property, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static HttpContent ToJsonContent(object body) =>
        new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
}

using System.Text;
using System.Text.Json;

namespace FitTrack.UI.Api;

/// <summary>Typed client for the fit-track HTTP API. The UI never talks to the domain directly.</summary>
public sealed class FitTrackApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();

    public Task RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken = default) =>
        PostAsync("auth/register", request, cancellationToken);

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<LoginResponse>("auth/login", request, cancellationToken);

    public Task<IReadOnlyList<MuscleGroupResponse>> GetMuscleGroupsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<MuscleGroupResponse>>("muscle-groups", cancellationToken);

    public Task<PagedResultResponse<MuscleGroupResponse>> GetMuscleGroupsPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultResponse<MuscleGroupResponse>>(
            ListPath("muscle-groups", search, page, pageSize),
            cancellationToken);

    public Task<MuscleGroupResponse> CreateMuscleGroupAsync(
        CreateMuscleGroupRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<MuscleGroupResponse>("muscle-groups", request, cancellationToken);

    public Task<MuscleGroupResponse> UpdateMuscleGroupAsync(
        Guid muscleGroupId,
        UpdateMuscleGroupRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<MuscleGroupResponse>($"muscle-groups/{muscleGroupId}", request, cancellationToken);

    public Task DeleteMuscleGroupAsync(Guid muscleGroupId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"muscle-groups/{muscleGroupId}", cancellationToken);

    public Task<IReadOnlyList<ExerciseResponse>> GetExercisesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ExerciseResponse>>("exercises", cancellationToken);

    public Task<PagedResultResponse<ExerciseResponse>> GetExercisesPageAsync(
        string? search,
        Guid? muscleGroupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultResponse<ExerciseResponse>>(
            ListPath("exercises", search, page, pageSize, muscleGroupId),
            cancellationToken);

    public Task<ExerciseResponse> CreateExerciseAsync(
        CreateExerciseRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ExerciseResponse>("exercises", request, cancellationToken);

    public Task<ExerciseResponse> UpdateExerciseAsync(
        Guid exerciseId,
        UpdateExerciseRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<ExerciseResponse>($"exercises/{exerciseId}", request, cancellationToken);

    public Task DeleteExerciseAsync(Guid exerciseId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"exercises/{exerciseId}", cancellationToken);

    public Task<IReadOnlyList<WorkoutSessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutSessionResponse>>("sessions", cancellationToken);

    public Task<WorkoutHistoryResponse> GetSessionsPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<WorkoutHistoryResponse>(
            ListPath("sessions", search, page, pageSize),
            cancellationToken);

    public Task<WorkoutSessionResponse> StartWorkoutAsync(
        StartWorkoutRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSessionResponse>("sessions", request, cancellationToken);

    /// <summary>
    /// Closes the workout: the exercises the athlete marked, the level they gave the session and what
    /// they wrote about it, all in one request.
    /// </summary>
    public Task<WorkoutSessionResponse> FinishWorkoutAsync(
        Guid sessionId,
        FinishWorkoutRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSessionResponse>($"sessions/{sessionId}/finish", request, cancellationToken);

    public Task CancelWorkoutAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"sessions/{sessionId}", cancellationToken);

    public Task<MyWorkoutSeriesResponse?> GetMyWorkoutSeriesAsync(CancellationToken cancellationToken = default) =>
        GetOptionalAsync<MyWorkoutSeriesResponse>("workout-series/mine", cancellationToken);

    public Task<IReadOnlyList<WorkoutSeriesResponse>> GetWorkoutSeriesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutSeriesResponse>>("workout-series", cancellationToken);

    public Task<PagedResultResponse<WorkoutSeriesResponse>> GetWorkoutSeriesPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultResponse<WorkoutSeriesResponse>>(
            ListPath("workout-series", search, page, pageSize),
            cancellationToken);

    public Task<WorkoutSeriesResponse> GetWorkoutSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default) =>
        GetAsync<WorkoutSeriesResponse>($"workout-series/{seriesId}", cancellationToken);

    public Task<WorkoutSeriesResponse> CreateWorkoutSeriesAsync(
        CreateWorkoutSeriesRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSeriesResponse>("workout-series", request, cancellationToken);

    /// <summary>
    /// Ends the cycle the athlete is in: the athlete is left without a series, so the trainer can
    /// create the next one when the time comes.
    /// </summary>
    public Task EndSeriesAsync(Guid athleteId, CancellationToken cancellationToken = default) =>
        PostAsync($"athletes/{athleteId}/series/end", new { }, cancellationToken);

    public Task<WorkoutSeriesResponse> RenameWorkoutSeriesAsync(
        Guid seriesId,
        SetWorkoutSeriesNameRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<WorkoutSeriesResponse>($"workout-series/{seriesId}", request, cancellationToken);

    public Task<WorkoutSeriesWorkoutResponse> AddSeriesWorkoutAsync(
        Guid seriesId,
        SetSeriesWorkoutRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WorkoutSeriesWorkoutResponse>($"workout-series/{seriesId}/workouts", request, cancellationToken);

    public Task<WorkoutSeriesWorkoutResponse> ChangeSeriesWorkoutAsync(
        Guid seriesId,
        Guid workoutId,
        SetSeriesWorkoutRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<WorkoutSeriesWorkoutResponse>($"workout-series/{seriesId}/workouts/{workoutId}", request, cancellationToken);

    public Task RemoveSeriesWorkoutAsync(
        Guid seriesId,
        Guid workoutId,
        CancellationToken cancellationToken = default) =>
        DeleteAsync($"workout-series/{seriesId}/workouts/{workoutId}", cancellationToken);



    public Task<IReadOnlyList<AthleteResponse>> GetAthletesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<AthleteResponse>>("athletes", cancellationToken);

    /// <summary>
    /// "<c>candidates</c>" flips the read to the link screen: athletes still linked to nobody,
    /// which is what the linking of either profile searches through.
    /// </summary>
    public Task<PagedResultResponse<AthleteResponse>> GetAthletesPageAsync(
        string? search,
        int page,
        int pageSize,
        bool candidates = false,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultResponse<AthleteResponse>>(
            ListPath("athletes", search, page, pageSize, candidates: candidates),
            cancellationToken);

    public Task LinkTrainerAsync(Guid athleteId, Guid? trainerId = null, CancellationToken cancellationToken = default) =>
        PostAsync($"athletes/{athleteId}/trainer", new LinkTrainerRequest(trainerId), cancellationToken);

    public Task UnlinkTrainerAsync(Guid athleteId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"athletes/{athleteId}/trainer", cancellationToken);

    public Task<IReadOnlyList<WorkoutSessionResponse>> GetAthleteWorkoutsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WorkoutSessionResponse>>($"athletes/{userId}/workouts", cancellationToken);

    public Task<WorkoutHistoryResponse> GetAthleteWorkoutsPageAsync(
        Guid userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<WorkoutHistoryResponse>(
            ListPath($"athletes/{userId}/workouts", search, page, pageSize),
            cancellationToken);

    public Task<IReadOnlyList<UserAccountResponse>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<UserAccountResponse>>("admin/users", cancellationToken);

    /// <summary>
    /// Every account holding one role, without pagination: the link screen searches them all, so the
    /// trainer is never out of reach because of the page size.
    /// </summary>
    public Task<IReadOnlyList<UserAccountResponse>> GetUsersByRoleAsync(
        string role,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<UserAccountResponse>>(
            $"admin/users?role={Uri.EscapeDataString(role)}",
            cancellationToken);

    public Task<PagedResultResponse<UserAccountResponse>> GetUsersPageAsync(
        string? search,
        int page,
        int pageSize,
        string? role = null,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultResponse<UserAccountResponse>>(
            ListPath("admin/users", search, page, pageSize, role: role),
            cancellationToken);

    public Task<UserAccountResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<UserAccountResponse>("admin/users", request, cancellationToken);

    public Task GrantRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default) =>
        PostAsync($"admin/users/{userId}/roles", new GrantRoleRequest(role), cancellationToken);

    public Task RevokeRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default) =>
        DeleteAsync($"admin/users/{userId}/roles/{role}", cancellationToken);

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
        PostAsync("auth/change-password", request, cancellationToken);

    public Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        PostAsync($"admin/users/{userId}/password", request, cancellationToken);

    /// <summary>
    /// Releases the account to build its own training plan, without a personal trainer. The release
    /// takes effect at once: it lives in the account and not in the token.
    /// </summary>
    public Task GrantSelfAuthoringAsync(Guid userId, CancellationToken cancellationToken = default) =>
        PostAsync($"admin/users/{userId}/self-authoring", new { }, cancellationToken);

    /// <summary>Takes the release back: the athlete needs a personal trainer again.</summary>
    public Task RevokeSelfAuthoringAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"admin/users/{userId}/self-authoring", cancellationToken);

    public async Task UploadAvatarAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "avatar", fileName);

        using var response = await httpClient.PostAsync("me/avatar", form, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteAvatarAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync("me/avatar", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<string?> GetMyAvatarAsync(CancellationToken cancellationToken = default) =>
        await GetAvatarDataUrlAsync("me/avatar", cancellationToken);

    public async Task<string?> GetUserAvatarAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await GetAvatarDataUrlAsync($"users/{userId}/avatar", cancellationToken);

    private async Task<string?> GetAvatarDataUrlAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string ListPath(
        string path,
        string? search,
        int page,
        int pageSize,
        Guid? muscleGroupId = null,
        bool candidates = false,
        string? role = null)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            parameters.Add("search=" + Uri.EscapeDataString(search.Trim()));
        }

        if (muscleGroupId is { } groupId && groupId != Guid.Empty)
        {
            parameters.Add("muscleGroupId=" + groupId);
        }

        if (candidates)
        {
            parameters.Add("candidates=true");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            parameters.Add("role=" + role);
        }

        parameters.Add("page=" + page);
        parameters.Add("pageSize=" + pageSize);

        return $"{path}?{string.Join('&', parameters)}";
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        return await ReadAsync<T>(response, cancellationToken);
    }

    /// <summary>
    /// Reads a resource that may legitimately be absent. The API answers <c>204</c> with no body when
    /// the athlete has no series yet, which is a state and not an error.
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
            ?? throw new ApiException(response.StatusCode, "O servidor respondeu sem conteúdo. Tente de novo.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var problem = ReadProblem(payload);

        throw new ApiException(
            response.StatusCode,
            problem.Message ?? "Não foi possível concluir a ação. Tente de novo.",
            problem.Code,
            problem.Errors);
    }

    /// <summary>
    /// The three pieces the screen takes from a <c>ProblemDetails</c>: the rule code (<c>title</c>),
    /// something to show (<c>detail</c>) and, on validation problems, the message for each field. A
    /// response that is not a known problem falls back to the HTTP reason.
    /// </summary>
    private static (string? Code, string? Message, IReadOnlyDictionary<string, string[]> Errors) ReadProblem(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return (null, null, NoErrors);
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null, NoErrors);
            }

            var errors = ReadErrors(root);
            var detail = ReadString(root, "detail") ?? ReadString(root, "message");

            var message = detail ?? (errors.Count > 0
                ? string.Join(' ', errors.SelectMany(pair => pair.Value))
                : ReadString(root, "title"));

            return (ReadString(root, "title"), message, errors);
        }
        catch (JsonException)
        {
            return (null, null, NoErrors);
        }
    }

    private static IReadOnlyDictionary<string, string[]> ReadErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return NoErrors;
        }

        var messages = new Dictionary<string, string[]>();

        foreach (var field in errors.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var values = field.Value.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString() ?? string.Empty)
                .Where(value => value.Length > 0)
                .ToArray();

            if (values.Length > 0)
            {
                messages[field.Name] = values;
            }
        }

        return messages;
    }

    private static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static HttpContent ToJsonContent(object body) =>
        new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
}

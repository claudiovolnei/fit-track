using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// The two modes of every collection endpoint: the plain array while no caller asks for pages, and the
/// envelope with the metadata once <c>page</c>/<c>pageSize</c> arrive. Search is case-insensitive and
/// matches by "contains".
/// </summary>
public sealed class PaginationFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task Catalog_KeepsTheArrayAndAddsTheEnvelope()
    {
        var trainer = await factory.TrainerAsync();
        var group = await trainer.CreateMuscleGroupAsync("Paginação Peito");
        var name = $"Paginação Supino {Guid.NewGuid():N}";

        var created = await trainer.PostAsJsonAsync("/exercises", new CreateExerciseRequest(name, group.Id));
        await created.EnsureSuccessWithBodyAsync();
        var exercise = (await created.Content.ReadFromJsonAsync<ExerciseResponse>())!;

        var array = await trainer.GetFromJsonAsync<IReadOnlyList<ExerciseResponse>>("/exercises");
        Assert.Contains(array!, item => item.Id == exercise.Id);

        var page = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            "/exercises?page=1&pageSize=1");

        Assert.NotNull(page);
        Assert.Single(page!.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.True(page.TotalCount >= 1);
        Assert.False(page.HasPrevious);
    }

    [RequiresPostgresFact]
    public async Task Catalog_SearchIgnoresCaseAndMatchesContains()
    {
        var trainer = await factory.TrainerAsync();
        var group = await trainer.CreateMuscleGroupAsync("Paginação Costas");
        var name = $"Paginação Remada {Guid.NewGuid():N}";

        var created = await trainer.PostAsJsonAsync("/exercises", new CreateExerciseRequest(name, group.Id));
        await created.EnsureSuccessWithBodyAsync();
        var exercise = (await created.Content.ReadFromJsonAsync<ExerciseResponse>())!;

        var searched = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            $"/exercises?search={Uri.EscapeDataString(name.ToUpperInvariant())}&page=1&pageSize=10");

        Assert.NotNull(searched);
        Assert.Equal(1, searched!.TotalCount);
        Assert.Equal(exercise.Id, Assert.Single(searched.Items).Id);

        // Without page/pageSize the same filter answers the compatible array.
        var filtered = await trainer.GetFromJsonAsync<IReadOnlyList<ExerciseResponse>>(
            $"/exercises?search={Uri.EscapeDataString(name)}");

        Assert.Contains(filtered!, item => item.Id == exercise.Id);
    }

    [RequiresPostgresFact]
    public async Task Catalog_TreatsThePercentSignAsText()
    {
        var trainer = await factory.TrainerAsync();
        var group = await trainer.CreateMuscleGroupAsync("Paginação Escape");
        var token = Guid.NewGuid().ToString("N")[..8];
        var literal = $"Cem % {token}";
        var wildcardWouldMatch = $"Cem X {token}";
        var underscoreLiteral = $"Cem _ {token}";
        var backslashLiteral = $"Cem \\ {token}";

        foreach (var name in new[] { literal, wildcardWouldMatch, underscoreLiteral, backslashLiteral })
        {
            var created = await trainer.PostAsJsonAsync("/exercises", new CreateExerciseRequest(name, group.Id));
            await created.EnsureSuccessWithBodyAsync();
        }

        var percent = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            $"/exercises?search={Uri.EscapeDataString($"% {token}")}&page=1&pageSize=10");

        Assert.NotNull(percent);
        Assert.Equal(1, percent!.TotalCount);
        Assert.Equal(literal, Assert.Single(percent.Items).Name);

        var underscore = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            $"/exercises?search={Uri.EscapeDataString($"_ {token}")}&page=1&pageSize=10");

        Assert.NotNull(underscore);
        Assert.Equal(1, underscore!.TotalCount);
        Assert.Equal(underscoreLiteral, Assert.Single(underscore.Items).Name);

        var backslash = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            $"/exercises?search={Uri.EscapeDataString($"\\ {token}")}&page=1&pageSize=10");

        Assert.NotNull(backslash);
        Assert.Equal(1, backslash!.TotalCount);
        Assert.Equal(backslashLiteral, Assert.Single(backslash.Items).Name);
    }

    [RequiresPostgresFact]
    public async Task Catalog_FiltersByMuscleGroupAndAnOutOfRangePageIsEmpty()
    {
        var trainer = await factory.TrainerAsync();
        var group = await trainer.CreateMuscleGroupAsync("Paginação Pernas");
        var name = $"Paginação Agachamento {Guid.NewGuid():N}";

        var created = await trainer.PostAsJsonAsync("/exercises", new CreateExerciseRequest(name, group.Id));
        await created.EnsureSuccessWithBodyAsync();

        var grouped = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            $"/exercises?muscleGroupId={group.Id}&page=1&pageSize=10");

        Assert.NotNull(grouped);
        Assert.Contains(grouped!.Items, item => item.Name == name);
        Assert.All(grouped.Items, item => Assert.Equal(group.Id, item.MuscleGroupId));

        // "Todos os grupos" sends an empty query value; the API treats it as no filter.
        var allGroups = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            "/exercises?muscleGroupId=&page=1&pageSize=10");

        Assert.NotNull(allGroups);
        Assert.True(allGroups!.TotalCount > 1);

        var beyond = await trainer.GetFromJsonAsync<PagedResultResponse<ExerciseResponse>>(
            "/exercises?page=999&pageSize=10");

        Assert.NotNull(beyond);
        Assert.Empty(beyond!.Items);
        Assert.True(beyond.TotalCount > 0);
    }

    [RequiresPostgresFact]
    public async Task Catalog_RefusesInvalidPaginationAndAnOverlongSearch()
    {
        var trainer = await factory.TrainerAsync();

        foreach (var query in new[] { "page=0", "pageSize=0", "pageSize=101" })
        {
            var response = await trainer.GetAsync($"/exercises?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var tooLong = new string('a', 81);
        var overlong = await trainer.GetAsync($"/exercises?search={tooLong}");

        Assert.Equal(HttpStatusCode.BadRequest, overlong.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Users_AreSearchedAndPagedForTheAdministrator()
    {
        var admin = await factory.CreateClient().SignInAsync(SeriesArrange.AdminEmail, SeriesArrange.AdminPassword);

        var page = await admin.GetFromJsonAsync<PagedResultResponse<UserAccountResponse>>(
            "/admin/users?search=admin&page=1&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(1, page!.Page);
        Assert.True(page.TotalCount >= 1);
        Assert.Contains(page.Items, account =>
            account.Email.Contains("admin", StringComparison.OrdinalIgnoreCase));
    }
}

using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

public sealed class CatalogFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    private const string AdminEmail = "admin@fittrack.local";
    private const string AdminPassword = "Admin123$";
    private const string TrainerEmail = "trainer@fittrack.local";
    private const string TrainerPassword = "Trainer123$";
    private const string AthletePassword = "Athlete123$";

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CanCreateAndListAnExercise()
    {
        var client = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var muscleGroup = await client.CreateMuscleGroupAsync("Catalog Chest");
        var name = $"Integration Press {Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync("/exercises", new CreateExerciseRequest(name, muscleGroup.Id));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var exercise = await created.Content.ReadFromJsonAsync<ExerciseResponse>();
        Assert.NotNull(exercise);
        Assert.True(exercise!.IsActive);
        Assert.Equal(muscleGroup.Id, exercise.MuscleGroupId);
        Assert.Equal(muscleGroup.Name, exercise.MuscleGroupName);

        var listed = await client.GetFromJsonAsync<IReadOnlyList<ExerciseResponse>>("/exercises");

        Assert.Contains(listed!, item => item.Id == exercise.Id);
    }

    [RequiresPostgresFact]
    public async Task ExerciseWithAnUnknownMuscleGroup_IsRefused()
    {
        var client = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);

        var response = await client.PostAsJsonAsync(
            "/exercises",
            new CreateExerciseRequest("Integration Ghost", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task PersonalTrainer_CanCreateAndRenameAMuscleGroup()
    {
        var client = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var muscleGroup = await client.CreateMuscleGroupAsync("Catalog Legs");
        var renamed = $"{muscleGroup.Name} II";

        var updated = await client.PutAsJsonAsync($"/muscle-groups/{muscleGroup.Id}", new { name = renamed });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var listed = await client.GetFromJsonAsync<IReadOnlyList<MuscleGroupResponse>>("/muscle-groups");

        Assert.Contains(listed!, item => item.Id == muscleGroup.Id && item.Name == renamed);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotCreateAMuscleGroup()
    {
        var client = factory.CreateClient();
        var email = $"athlete-{Guid.NewGuid():N}@fittrack.local";

        await client.RegisterAsync(email, AthletePassword);
        await client.SignInAsync(email, AthletePassword);

        var response = await client.PostAsJsonAsync("/muscle-groups", new { name = "Not allowed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CanReadTheMuscleGroups()
    {
        var client = factory.CreateClient();
        var email = $"athlete-{Guid.NewGuid():N}@fittrack.local";

        await client.RegisterAsync(email, AthletePassword);
        await client.SignInAsync(email, AthletePassword);

        var response = await client.GetAsync("/muscle-groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task AdminWithoutTheTrainerProfile_CannotCreateAnExercise()
    {
        var client = await factory.CreateClient().SignInAsync(AdminEmail, AdminPassword);

        var response = await client.PostAsJsonAsync(
            "/exercises",
            new CreateExerciseRequest("Not allowed", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotCreateAnExercise()
    {
        var client = factory.CreateClient();
        var email = $"athlete-{Guid.NewGuid():N}@fittrack.local";

        await client.RegisterAsync(email, AthletePassword);
        await client.SignInAsync(email, AthletePassword);

        var response = await client.PostAsJsonAsync(
            "/exercises",
            new CreateExerciseRequest("Not allowed", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Anonymous_IsRejectedOnTheCatalog()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/exercises");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>The planning slice end to end, against the real API and a real PostgreSQL database.</summary>
public sealed class WorkoutPlanFlowTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    private const string TrainerEmail = "trainer@fittrack.local";
    private const string TrainerPassword = "Trainer123$";

    [RequiresPostgresFact]
    public async Task Trainer_AssignsAPlanAndBuildsTheOptions()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var athlete = await factory.CreateAthleteAsync();
        var chest = await trainer.CreateMuscleGroupAsync("Plan Chest");
        var back = await trainer.CreateMuscleGroupAsync("Plan Back");

        var assigned = await trainer.PostAsJsonAsync("/workout-plans", new AssignWorkoutPlanRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);
        var plan = (await assigned.Content.ReadFromJsonAsync<WorkoutPlanResponse>())!;
        Assert.Equal(athlete.UserId, plan.UserId);
        Assert.Empty(plan.Options);

        // Assigning again keeps the same plan instead of creating a second one.
        var again = await trainer.PostAsJsonAsync("/workout-plans", new AssignWorkoutPlanRequest(athlete.UserId));

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(plan.Id, (await again.Content.ReadFromJsonAsync<WorkoutPlanResponse>())!.Id);

        var first = await trainer.AddPlanOptionAsync(plan.Id, chest.Id);
        var second = await trainer.AddPlanOptionAsync(plan.Id, back.Id, chest.Id);

        Assert.Equal(1, first.Position);
        Assert.Equal(2, second.Position);
        Assert.StartsWith("Plan Chest", Assert.Single(first.MuscleGroups).Name);

        // The stored order survives the round trip: options come back by position, not by key.
        var reloaded = await trainer.GetFromJsonAsync<WorkoutPlanResponse>($"/workout-plans/{plan.Id}");

        Assert.NotNull(reloaded);
        Assert.Equal([1, 2], reloaded.Options.Select(option => option.Position));
        Assert.Equal(plan.Id, reloaded.Id);
        Assert.Equal(2, reloaded.Options[1].MuscleGroups.Count);

        // Editing an option replaces its groups without touching the other one.
        var changed = await trainer.PutAsJsonAsync(
            $"/workout-plans/{plan.Id}/options/{second.Id}",
            new SetWorkoutPlanOptionRequest([back.Id]));

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var changedOption = (await changed.Content.ReadFromJsonAsync<WorkoutPlanOptionResponse>())!;
        Assert.Equal(back.Id, Assert.Single(changedOption.MuscleGroups).Id);

        // Removing an option renumbers the rest, so the trainer never sees a hole.
        var removed = await trainer.DeleteAsync($"/workout-plans/{plan.Id}/options/{second.Id}");

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        var afterRemoval = await trainer.GetFromJsonAsync<WorkoutPlanResponse>($"/workout-plans/{plan.Id}");
        Assert.Equal([1], afterRemoval!.Options.Select(option => option.Position));

        var plans = await trainer.GetFromJsonAsync<IReadOnlyList<WorkoutPlanResponse>>("/workout-plans");
        Assert.Contains(plans!, candidate => candidate.Id == plan.Id);
    }

    [RequiresPostgresFact]
    public async Task Trainer_CannotAssignAPlanToAnUnknownAccount()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);

        var response = await trainer.PostAsJsonAsync("/workout-plans", new AssignWorkoutPlanRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Trainer_CannotReadAPlanThatDoesNotExist()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);

        var response = await trainer.GetAsync($"/workout-plans/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotBuildPlans()
    {
        var athlete = await factory.CreateAthleteAsync();

        var list = await athlete.Client.GetAsync("/workout-plans");
        var assign = await athlete.Client.PostAsJsonAsync(
            "/workout-plans",
            new AssignWorkoutPlanRequest(Guid.NewGuid()));
        var athletes = await athlete.Client.GetAsync("/athletes");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, assign.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, athletes.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Athlete_WithoutAPlan_ReadsNothing()
    {
        var athlete = await factory.CreateAthleteAsync();

        var response = await athlete.Client.GetAsync("/workout-plan");

        // The athlete has no plan yet: 200 with an empty body is the "nothing to train" answer.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(string.IsNullOrWhiteSpace(await response.Content.ReadAsStringAsync()));
    }

    [RequiresPostgresFact]
    public async Task Athlete_ReadsThePlanTheTrainerAssigned()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var athlete = await factory.CreateAthleteAsync();
        var legs = await trainer.CreateMuscleGroupAsync("Plan Legs");
        var plan = await trainer.AssignPlanAsync(athlete.UserId);
        await trainer.AddPlanOptionAsync(plan.Id, legs.Id);

        var mine = await athlete.Client.ReadMyPlanAsync();

        Assert.Equal(athlete.UserId, mine.UserId);
        Assert.Equal(plan.TrainerId, mine.TrainerId);
        var option = Assert.Single(mine.Options);
        Assert.StartsWith("Plan Legs", Assert.Single(option.MuscleGroups).Name);
        Assert.Null(option.Today);
    }

    [RequiresPostgresFact]
    public async Task Trainer_ListsTheAthletesAndTheirWorkouts()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var athlete = await factory.CreateAthleteAsync();

        var athletes = await trainer.GetFromJsonAsync<IReadOnlyList<AthleteResponse>>("/athletes");
        var workouts = await trainer.GetFromJsonAsync<IReadOnlyList<WorkoutSessionResponse>>(
            $"/athletes/{athlete.UserId}/workouts");

        Assert.Contains(athletes!, candidate => candidate.UserId == athlete.UserId);
        Assert.NotNull(workouts);
    }

    [RequiresPostgresFact]
    public async Task Athlete_CannotReadAnotherAthletesWorkouts()
    {
        var trainer = await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);
        var athlete = await factory.CreateAthleteAsync();
        var other = await factory.CreateAthleteAsync();

        // The trainer may follow whoever they coach; the other athlete may not.
        var asTrainer = await trainer.GetAsync($"/athletes/{athlete.UserId}/workouts");
        var asAthlete = await other.Client.GetAsync($"/athletes/{athlete.UserId}/workouts");

        Assert.Equal(HttpStatusCode.OK, asTrainer.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asAthlete.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Anonymous_CannotReadOrBuildPlans()
    {
        var anonymous = factory.CreateClient();

        var plan = await anonymous.GetAsync("/workout-plan");
        var assign = await anonymous.PostAsJsonAsync("/workout-plans", new AssignWorkoutPlanRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, plan.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, assign.StatusCode);
    }
}

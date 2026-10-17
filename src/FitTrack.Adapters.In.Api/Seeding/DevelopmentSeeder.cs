using FitTrack.Adapters.Out.Persistence;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.In.Api.Seeding;

/// <summary>
/// Development-only accounts and roles, so the acceptance flow can run without manual setup.
/// Never invoked outside the Development environment.
/// </summary>
internal static class DevelopmentSeeder
{
    public const string AdminEmail = "admin@fittrack.local";

    public const string AdminPassword = "Admin123$";

    public const string TrainerEmail = "trainer@fittrack.local";

    public const string TrainerPassword = "Trainer123$";

    public const string AthleteEmail = "athlete@fittrack.local";

    public const string AthletePassword = "Athlete123$";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        await EnsureAccountAsync(users, AdminEmail, AdminPassword, Roles.Admin);
        await EnsureAccountAsync(users, TrainerEmail, TrainerPassword, Roles.PersonalTrainer);
        await EnsureAccountAsync(users, AthleteEmail, AthletePassword, Roles.User);

        await EnsureCatalogAsync(services.GetRequiredService<FitTrackDbContext>());
    }

    /// <summary>
    /// Starter catalog so the trainer does not face an empty screen. Written straight through the
    /// context on purpose: the seed has no authenticated caller to go through the use cases.
    /// </summary>
    private static async Task EnsureCatalogAsync(FitTrackDbContext dbContext)
    {
        (string MuscleGroup, string[] Exercises)[] catalog =
        [
            ("Chest", ["Bench Press", "Incline Bench Press"]),
            ("Back", ["Barbell Row", "Lat Pulldown"]),
            ("Legs", ["Back Squat", "Leg Press"]),
            ("Biceps", ["Barbell Curl"]),
            ("Triceps", ["Triceps Pushdown"]),
        ];

        foreach (var (muscleGroupName, exerciseNames) in catalog)
        {
            var muscleGroup = await dbContext.MuscleGroups
                .FirstOrDefaultAsync(candidate => candidate.Name == muscleGroupName);

            if (muscleGroup is null)
            {
                muscleGroup = new MuscleGroupRecord { Id = Guid.NewGuid(), Name = muscleGroupName };
                dbContext.MuscleGroups.Add(muscleGroup);
            }

            foreach (var exerciseName in exerciseNames)
            {
                if (await dbContext.Exercises.AnyAsync(exercise => exercise.Name == exerciseName))
                {
                    continue;
                }

                dbContext.Exercises.Add(new ExerciseRecord
                {
                    Id = Guid.NewGuid(),
                    Name = exerciseName,
                    MuscleGroupId = muscleGroup.Id,
                    IsActive = true,
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureAccountAsync(
        UserManager<AppUser> users,
        string email,
        string password,
        string role)
    {
        var account = await users.FindByEmailAsync(email);
        if (account is not null)
        {
            return;
        }

        account = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var created = await users.CreateAsync(account, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not seed the development account '{email}': " +
                string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        await users.AddToRoleAsync(account, role);
    }
}

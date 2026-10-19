using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Endpoints;
using FitTrack.Adapters.In.Api.Seeding;
using FitTrack.Adapters.Out.Persistence;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The DbContext connection is owned by the composition root; Aspire supplies it by name.
builder.AddNpgsqlDbContext<FitTrackDbContext>("fittrack");

// Outbound adapters: repositories, query ports, clock, and id generator behind their ports.
builder.Services.AddOutAdapters();

// Application use cases: one registration per actor intention.
builder.Services.AddScoped<ICreateExercise, CreateExercise>();
builder.Services.AddScoped<IListExercises, ListExercises>();
builder.Services.AddScoped<IUpdateExercise, UpdateExercise>();
builder.Services.AddScoped<ICreateMuscleGroup, CreateMuscleGroup>();
builder.Services.AddScoped<IListMuscleGroups, ListMuscleGroups>();
builder.Services.AddScoped<IUpdateMuscleGroup, UpdateMuscleGroup>();
builder.Services.AddScoped<IListUserSessions, ListUserSessions>();
builder.Services.AddScoped<IListAthletes, ListAthletes>();
builder.Services.AddScoped<IListUsers, ListUsers>();
builder.Services.AddScoped<ICreateWorkoutSeries, CreateWorkoutSeries>();
builder.Services.AddScoped<IListWorkoutSeries, ListWorkoutSeries>();
builder.Services.AddScoped<IGetWorkoutSeries, GetWorkoutSeries>();
builder.Services.AddScoped<IRenameWorkoutSeries, RenameWorkoutSeries>();
builder.Services.AddScoped<IAddSeriesWorkout, AddSeriesWorkout>();
builder.Services.AddScoped<IChangeSeriesWorkout, ChangeSeriesWorkout>();
builder.Services.AddScoped<IRemoveSeriesWorkout, RemoveSeriesWorkout>();
builder.Services.AddScoped<IAssignWorkoutSeries, AssignWorkoutSeries>();
builder.Services.AddScoped<IListAthleteWorkouts, ListAthleteWorkouts>();
builder.Services.AddScoped<IGetMyWorkoutSeries, GetMyWorkoutSeries>();
builder.Services.AddScoped<IStartWorkout, StartWorkout>();
builder.Services.AddScoped<ILogPerformedSet, LogPerformedSet>();
builder.Services.AddScoped<IFinishWorkout, FinishWorkout>();
builder.Services.AddScoped<ICancelWorkout, CancelWorkout>();

// The current user is an inbound concern: it reads the already validated JWT.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing.");
if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException($"Configuration value '{JwtOptions.SectionName}:SigningKey' must be set.");
}

builder.Services.AddSingleton<JwtTokenIssuer>();

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<FitTrackDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.PersonalTrainerOnly, policy => policy.RequireRole(Roles.PersonalTrainer));
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// The browser client is served from its own origin, so the API must answer preflight requests.
var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(cors.AllowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Documentation UI on /scalar, reading the document the API already exposes on /openapi/v1.json.
    // Development-only, like the seeded accounts.
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<FitTrackDbContext>().Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapAvatarEndpoints();
app.MapAdminEndpoints();
app.MapExerciseEndpoints();
app.MapMuscleGroupEndpoints();
app.MapSessionEndpoints();
app.MapWorkoutSeriesEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real API through WebApplicationFactory.</summary>
public partial class Program;


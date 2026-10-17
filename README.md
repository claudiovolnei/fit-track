# FitTrack

Fitness tracking platform: a personal trainer builds workout plans out of a muscle-group/exercise
catalog and assigns them to athletes; athletes start a session from their assigned plan and log the
sets they actually performed.

## Stack

- .NET 10
- .NET Aspire 13 for local orchestration (PostgreSQL, API, web client)
- ASP.NET Core Minimal APIs with OpenAPI and JWT bearer authentication
- ASP.NET Core Identity backed by EF Core stores for accounts and roles
- EF Core 10 with the Npgsql provider
- Blazor WebAssembly for the client
- xUnit for unit and integration tests

## Architecture

Hexagonal (ports and adapters) with tactical Domain-Driven Design. The dependency rule always
points inward: adapters depend on Application, Application depends on Domain, and Domain depends on
nothing.

| Project | Role |
| --- | --- |
| `FitTrack.Domain` | Entities, value objects and invariants (`WorkoutCatalog`, `WorkoutPlanning`, `WorkoutExecution`, `Identity`) |
| `FitTrack.Application` | Use cases (one class per actor intention), inbound ports, outbound ports, DTOs and mapping |
| `FitTrack.Adapters.In.Api` | Inbound adapter: endpoints, request contracts, authentication, composition root |
| `FitTrack.Adapters.Out.Persistence` | Outbound adapter: `FitTrackDbContext`, records, repositories, query services, migrations |
| `FitTrack.UI` | Blazor components and client-side services |
| `FitTrack.Web` | Blazor WebAssembly host |
| `FitTrack.ServiceDefaults` | Aspire observability and resilience defaults shared by the services |
| `FitTrack.AppHost` | Aspire orchestration of PostgreSQL, the API and the web client |

## Getting started

Prerequisites: the .NET 10 SDK and a container runtime (Docker Desktop or Podman) so Aspire can start
PostgreSQL.

```bash
dotnet run --project src/FitTrack.AppHost
```

- Web client: <http://localhost:5090>
- API: <http://localhost:5080> (OpenAPI document at `/openapi/v1.json` in Development)

On startup in Development the API applies the EF Core migrations and seeds a starter catalog plus
three accounts, so the flow can be exercised without manual setup:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@fittrack.local` | `Admin123$` |
| Personal trainer | `trainer@fittrack.local` | `Trainer123$` |
| Athlete | `athlete@fittrack.local` | `Athlete123$` |

The signing key and database credentials committed in `appsettings.Development.json` are
development-only. Real environments must supply `Jwt:SigningKey` and the connection string as
configuration or secrets.

## Tests

```bash
dotnet test
```

The unit tests run standalone. The integration tests boot the real API through
`WebApplicationFactory`; the ones that need a database are skipped unless `FITTRACK_TEST_DB` holds a
PostgreSQL connection string.

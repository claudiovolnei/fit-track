# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore the API's dependency graph before copying source, so NuGet is cached in its own layer.
COPY src/FitTrack.Domain/FitTrack.Domain.csproj src/FitTrack.Domain/
COPY src/FitTrack.Application/FitTrack.Application.csproj src/FitTrack.Application/
COPY src/FitTrack.Adapters.Out.Persistence/FitTrack.Adapters.Out.Persistence.csproj src/FitTrack.Adapters.Out.Persistence/
COPY src/FitTrack.ServiceDefaults/FitTrack.ServiceDefaults.csproj src/FitTrack.ServiceDefaults/
COPY src/FitTrack.Adapters.In.Api/FitTrack.Adapters.In.Api.csproj src/FitTrack.Adapters.In.Api/
RUN dotnet restore src/FitTrack.Adapters.In.Api/FitTrack.Adapters.In.Api.csproj

COPY src/ src/
RUN dotnet publish src/FitTrack.Adapters.In.Api/FitTrack.Adapters.In.Api.csproj \
    --configuration Release \
    --output /app \
    --no-restore \
    --nologo

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Fly terminates TLS and forwards over HTTP to this port.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_NOLOGO=true

EXPOSE 8080

COPY --from=build /app .

# The base image defines APP_UID; the API keeps no state on disk (avatars live in PostgreSQL).
USER $APP_UID

ENTRYPOINT ["dotnet", "FitTrack.Adapters.In.Api.dll"]

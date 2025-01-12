# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Traefik.Middelware.Api/Traefik.Middelware.Api.csproj", "Traefik.Middelware.Api/"]
RUN dotnet restore "./Traefik.Middelware.Api/Traefik.Middelware.Api.csproj"
COPY . .
WORKDIR "/src/Traefik.Middelware.Api"
RUN dotnet build "./Traefik.Middelware.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Traefik.Middelware.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Temporarily switch to root user to install curl and clean up
USER root

# Install curl and clean up
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Switch back to the original user (if needed)
USER $APP_UID

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Traefik.Middelware.Api.dll"]
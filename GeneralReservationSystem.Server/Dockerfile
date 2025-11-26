# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y curl
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=10 CMD curl -f http://localhost:8080/api/health

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GeneralReservationSystem.Server/GeneralReservationSystem.Server.csproj", "GeneralReservationSystem.Server/"]
RUN dotnet restore "./GeneralReservationSystem.Server/GeneralReservationSystem.Server.csproj"
COPY . .
WORKDIR "/src/GeneralReservationSystem.Server"
RUN dotnet build "./GeneralReservationSystem.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GeneralReservationSystem.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GeneralReservationSystem.Server.dll"]
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pathfinder.example.client/pathfinder.example.client.csproj", "pathfinder.example.client/"]
COPY ["pathfinder.example.ServiceDefaults/pathfinder.example.ServiceDefaults.csproj", "pathfinder.example.ServiceDefaults/"]
RUN dotnet restore "pathfinder.example.client/pathfinder.example.client.csproj"
COPY . .
WORKDIR "/src/pathfinder.example.client"
RUN dotnet build "./pathfinder.example.client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./pathfinder.example.client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pathfinder.example.client.dll"]

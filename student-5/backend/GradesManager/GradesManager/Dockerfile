FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/GradesManager/GradesManager/GradesManager.csproj", "backend/GradesManager/GradesManager/"]
COPY ["contracts/Contracts/Contracts.csproj", "contracts/Contracts/"]
RUN dotnet restore "backend/GradesManager/GradesManager/GradesManager.csproj"
COPY . .
WORKDIR "/src/backend/GradesManager/GradesManager"
RUN dotnet build "./GradesManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GradesManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

ENTRYPOINT ["dotnet", "GradesManager.dll"]
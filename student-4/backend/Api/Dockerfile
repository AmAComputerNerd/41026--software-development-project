FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["contracts/Contracts/Contracts.csproj", "contracts/Contracts/"]
COPY ["backend/Api/Api.csproj", "backend/Api/"]
RUN dotnet restore "backend/Api/Api.csproj"
COPY contracts/Contracts contracts/Contracts
COPY backend/Api backend/Api
WORKDIR "/src/backend/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

ENTRYPOINT ["dotnet", "Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["contracts/Contracts/Contracts.csproj", "contracts/Contracts/"]
COPY ["database/Database/Database.csproj", "database/Database/"]
RUN dotnet restore "database/Database/Database.csproj"
COPY contracts/Contracts contracts/Contracts
COPY database/Database database/Database
WORKDIR "/src/database/Database"
RUN dotnet build "./Database.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Database.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/Data \
    && chown -R $APP_UID:$APP_UID /app/Data

USER $APP_UID

ENTRYPOINT ["dotnet", "Database.dll"]

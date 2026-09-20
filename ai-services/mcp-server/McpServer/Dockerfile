FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["McpServer/McpServer.csproj", "McpServer/"]
RUN dotnet restore "McpServer/McpServer.csproj"
COPY . .
WORKDIR "/src/McpServer"
RUN dotnet build "McpServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "McpServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
RUN chown -R $APP_UID:$APP_UID /app

USER $APP_UID

ENTRYPOINT ["dotnet", "McpServer.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY McpServer/McpServer.csproj McpServer/
RUN dotnet restore McpServer/McpServer.csproj
COPY . .
RUN dotnet publish McpServer/McpServer.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore -warnaserror

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "McpServer.dll"]

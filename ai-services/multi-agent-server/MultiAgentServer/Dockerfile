FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MultiAgentServer.csproj .
RUN dotnet restore MultiAgentServer.csproj
COPY . .
RUN dotnet publish MultiAgentServer.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore -warnaserror

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "MultiAgentServer.dll"]

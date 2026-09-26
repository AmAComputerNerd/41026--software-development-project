FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ai-services/rag-server/RagServer/RagServer.csproj", "ai-services/rag-server/RagServer/"]
RUN dotnet restore "ai-services/rag-server/RagServer/RagServer.csproj"
COPY ai-services/rag-server/RagServer/ ai-services/rag-server/RagServer/
WORKDIR "/src/ai-services/rag-server/RagServer"
RUN dotnet build "RagServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RagServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY AGENTS.md ./Corpus/AGENTS.md
COPY docs/architecture/data-flows.md ./Corpus/docs/architecture/data-flows.md
COPY student-3/README.md ./Corpus/student-3/README.md

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
RUN chown -R $APP_UID:$APP_UID /app

USER $APP_UID

ENTRYPOINT ["dotnet", "RagServer.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ai-services/rag-server/RagServer/RagServer.csproj RagServer/
RUN dotnet restore RagServer/RagServer.csproj
COPY ai-services/rag-server/RagServer/ RagServer/
RUN dotnet publish RagServer/RagServer.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore -warnaserror

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY AGENTS.md Corpus/
COPY docs/architecture/data-flows.md Corpus/docs/architecture/
COPY docs/knowledge-base/course_policies.md Corpus/docs/knowledge-base/
COPY student-1/README.md Corpus/student-1/
COPY student-2/README.md Corpus/student-2/
COPY student-3/README.md Corpus/student-3/
COPY student-3/docs/deadline-help.md Corpus/student-3/docs/
COPY student-4/README.md Corpus/student-4/
COPY student-4/docs/account-help.md Corpus/student-4/docs/
COPY student-5/README.md Corpus/student-5/
USER $APP_UID
ENTRYPOINT ["dotnet", "RagServer.dll"]

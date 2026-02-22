# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/RepoIndexer/RepoIndexer.csproj ./
RUN dotnet restore

COPY src/RepoIndexer/ ./
RUN dotnet publish -c Release -o /out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /out ./

# Default root folder mountpoint — override via Docker volume + argument
ENTRYPOINT ["dotnet", "RepoIndexer.dll"]
CMD ["/repos"]

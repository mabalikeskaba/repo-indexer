# repo-indexer

## Project Description

The `repo-indexer` is a service that runs in a Docker container and monitors a root directory containing multiple repositories. The goal is to index the repositories to reduce the context size for agent requests.

## How It Works

- **Initial Indexing:** All repositories in the root directory are indexed at startup.
- **FileWatcher:** Changes to `.cs` files trigger an automatic re-indexing.
- **Indexing:**
  - A `.repo-indexer` folder is created in the root of each repository.
  - A `repo.json` file is generated, containing all `.cs` files, classes, methods, and summaries.
- **Exclusions:** Directories like `bin/`, `debug/`, and `.repo-indexer/` are ignored.

## Technology Stack

- **Language / Framework:** .NET C#
- **Analysis Tool:** Roslyn
- **Runtime Environment:** Docker Container

## Project Structure

```
repo-indexer/
├── Dockerfile
├── .dockerignore
└── src/RepoIndexer/
    ├── RepoIndexer.csproj
    ├── Program.cs
    ├── Models/
    │   └── IndexModels.cs          – RepoIndex, FileIndex, ClassIndex
    └── Services/
        ├── WatcherOptions.cs       – Configuration (RootFolder)
        ├── RoslynAnalyzerService.cs – Roslyn analysis: classes, summaries, methods
        ├── RepoIndexingService.cs  – Orchestration & writing the repo.json
        └── RepoWatcherService.cs   – FileWatcher + debouncing + initial scan
```

## Usage

### With .NET CLI

1. Build the project:
   ```bash
   dotnet build src/RepoIndexer/RepoIndexer.csproj
   ```
2. Start the service:
   ```bash
   dotnet src/RepoIndexer/bin/Debug/net8.0/RepoIndexer.dll /path/to/repos
   ```

### With Docker

1. Build the Docker image:
   ```bash
   docker build -t repo-indexer .
   ```
2. Start the container:
   ```bash
   docker run -v /local/repos:/repos repo-indexer /repos
   ```

## Example of `repo.json`

```json
{
  "repoName": "my-repo",
  "generatedAt": "2026-02-22T10:00:00Z",
  "files": [
    {
      "path": "src/Services/MyService.cs",
      "classes": [
        {
          "name": "MyService",
          "summary": "Provides core functionality for processing data.",
          "methods": ["ProcessData", "GetResult"]
        }
      ]
    }
  ]
}
```

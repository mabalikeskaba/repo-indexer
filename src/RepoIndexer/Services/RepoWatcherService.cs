using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RepoIndexer.Services;

public class RepoWatcherService(
    WatcherOptions options,
    RepoIndexingService indexingService,
    ILogger<RepoWatcherService> logger) : BackgroundService
{
    // Debounce delay: wait this long after the last change before re-indexing
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(2);

    // Maps repoRoot -> active debounce CancellationTokenSource
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceMap = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial full indexing of all repos found at startup
        IndexAllRepos();

        using var watcher = new FileSystemWatcher(options.RootFolder)
        {
            Filter = "*.cs",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;

        logger.LogInformation("FileWatcher active on: {Root}", options.RootFolder);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    private void OnChanged(object _, FileSystemEventArgs e) => ScheduleReindex(e.FullPath);

    private void OnRenamed(object _, RenamedEventArgs e) => ScheduleReindex(e.FullPath);

    private void ScheduleReindex(string changedFilePath)
    {
        var repoRoot = FindRepoRoot(changedFilePath);
        if (repoRoot is null)
        {
            logger.LogDebug("No repo root found for changed file: {File}", changedFilePath);
            return;
        }

        // Cancel any existing debounce for this repo and start a new one
        if (_debounceMap.TryRemove(repoRoot, out var existingCts))
            existingCts.Cancel();

        var cts = new CancellationTokenSource();
        _debounceMap[repoRoot] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cts.Token);
                _debounceMap.TryRemove(repoRoot, out _);
                indexingService.IndexRepo(repoRoot);
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer change — do nothing
            }
        }, cts.Token);
    }

    private void IndexAllRepos()
    {
        logger.LogInformation("Starting initial indexing...");

        var repos = FindAllRepos(options.RootFolder);
        foreach (var repo in repos)
            indexingService.IndexRepo(repo);

        logger.LogInformation("Initial indexing complete. Found {Count} repo(s).", repos.Count);
    }

    private static List<string> FindAllRepos(string rootFolder)
    {
        var repos = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(rootFolder))
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                repos.Add(dir);
        }

        return repos;
    }

    /// <summary>
    /// Walks up from the given file path to find the nearest directory
    /// containing a .git folder, without leaving the root folder.
    /// </summary>
    private string? FindRepoRoot(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);

        while (dir is not null && dir.StartsWith(options.RootFolder, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}

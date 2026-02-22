using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RepoIndexer.Services;

/// <summary>
/// Background service that monitors the root folder for .cs file changes
/// and triggers debounced re-indexing of the affected repository.
/// </summary>
public class RepoWatcherService(
    WatcherOptions options,
    RepoIndexingService indexingService,
    RepoDiscoveryService discoveryService,
    ILogger<RepoWatcherService> logger) : BackgroundService
{
    // Debounce delay: wait this long after the last change before re-indexing
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(2);

    // Maps repoRoot -> active debounce CancellationTokenSource
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceMap = new();

    /// <summary>
    /// Performs the initial full indexing and activates the FileWatcher for ongoing change detection.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial full indexing of all repos found at startup
        discoveryService.DiscoverAndIndex(options.RootFolder);

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

    /// <summary>
    /// Walks up the directory tree from a changed file to find the nearest repository root containing a .git folder.
    /// Returns null if no repository root is found within the watched root folder.
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

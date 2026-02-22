namespace RepoIndexer.Services;

/// <summary>
/// Configuration options for the repository watcher service.
/// </summary>
public class WatcherOptions
{
    /// <summary>
    /// The root folder to monitor for repository changes.
    /// </summary>
    public required string RootFolder { get; init; }
}

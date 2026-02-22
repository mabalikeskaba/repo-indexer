using Microsoft.Extensions.Logging;

namespace RepoIndexer.Services;

/// <summary>
/// Discovers repositories within a root folder recursively and triggers their initial indexing with a console progress display.
/// </summary>
public class RepoDiscoveryService(RepoIndexingService indexingService, ILogger<RepoDiscoveryService> logger)
{
    private static readonly HashSet<string> ExcludedScanFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "packages", "node_modules", ".git", ".repo-indexer"
    };

    /// <summary>
    /// Scans the given root folder recursively for repositories and indexes each one,
    /// displaying a progress bar in the console.
    /// </summary>
    /// <param name="rootFolder">The root folder to scan for repositories.</param>
    public void DiscoverAndIndex(string rootFolder)
    {
        Console.WriteLine("Scanning for repositories...");
        logger.LogInformation("Starting initial indexing of: {Root}", rootFolder);

        var repos = FindAllReposRecursive(rootFolder);

        if (repos.Count == 0)
        {
            Console.WriteLine("No repositories found.");
            logger.LogWarning("No repositories found in: {Root}", rootFolder);
            return;
        }

        Console.WriteLine($"Found {repos.Count} repository/repositories. Starting indexing...\n");

        for (int i = 0; i < repos.Count; i++)
        {
            var repo = repos[i];
            var repoName = Path.GetFileName(repo);

            indexingService.IndexRepo(repo);

            int progress = (int)Math.Round((i + 1) / (double)repos.Count * 100);
            int filled = progress / 5;
            var bar = $"[{new string('#', filled)}{new string('-', 20 - filled)}] {progress,3}%";
            Console.Write($"\r  {bar}  ({i + 1}/{repos.Count}) {repoName,-40}");
        }

        Console.WriteLine($"\nInitial indexing complete. {repos.Count} repo(s) indexed.");
        logger.LogInformation("Initial indexing complete. {Count} repo(s) indexed.", repos.Count);
    }

    /// <summary>
    /// Recursively searches the given folder for directories containing a .git folder,
    /// excluding known non-repository directories.
    /// </summary>
    /// <param name="rootFolder">The root folder to search.</param>
    /// <returns>A list of absolute paths to discovered repository roots.</returns>
    public List<string> FindAllReposRecursive(string rootFolder)
    {
        var repos = new List<string>();
        SearchDir(rootFolder, repos);
        return repos;
    }

    private static void SearchDir(string dir, List<string> repos)
    {
        // If this directory is a repo, add it and stop recursing into it
        if (Directory.Exists(Path.Combine(dir, ".git")))
        {
            repos.Add(dir);
            return;
        }

        try
        {
            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(sub);
                if (ExcludedScanFolders.Contains(name))
                    continue;

                SearchDir(sub, repos);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we cannot access
        }
    }
}

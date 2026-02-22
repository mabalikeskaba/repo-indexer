using Microsoft.Extensions.Logging;
using RepoIndexer.Models;
using System.Text.Json;

namespace RepoIndexer.Services;

/// <summary>
/// Orchestrates the indexing of a repository by collecting all .cs files,
/// analyzing them with Roslyn, and writing the result to a repo.json file.
/// </summary>
public class RepoIndexingService(RoslynAnalyzerService analyzer, ILogger<RepoIndexingService> logger)
{
    private static readonly string IndexFolder = ".repo-indexer";
    private static readonly string IndexFile = "repo.json";

    private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "debug"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Indexes the specified repository and generates a repo.json file.
    /// </summary>
    /// <param name="repoRoot">The root directory of the repository to index.</param>
    public void IndexRepo(string repoRoot)
    {
        logger.LogInformation("Indexing repo: {Repo}", repoRoot);

        var files = Directory
            .EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f, repoRoot))
            .ToList();

        var fileIndexes = new List<FileIndex>();

        foreach (var file in files)
        {
            var index = analyzer.AnalyzeFile(file, repoRoot);
            if (index is not null)
                fileIndexes.Add(index);
        }

        var repoIndex = new RepoIndex
        {
            RepoName = Path.GetFileName(repoRoot),
            GeneratedAt = DateTime.UtcNow,
            Files = fileIndexes
        };

        WriteIndex(repoRoot, repoIndex);

        logger.LogInformation(
            "Indexed {FileCount} files with {ClassCount} classes in {Repo}",
            fileIndexes.Count,
            fileIndexes.Sum(f => f.Classes.Count),
            repoRoot);
    }

    private static bool IsExcluded(string filePath, string repoRoot)
    {
        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Exclude if any path segment matches excluded folder names
        if (segments.Any(s => ExcludedFolders.Contains(s)))
            return true;

        // Exclude package.json (shouldn't be .cs but kept for consistency)
        if (Path.GetFileName(filePath).Equals("package.json", StringComparison.OrdinalIgnoreCase))
            return true;

        // Exclude the .repo-indexer folder itself
        if (segments.Any(s => s.Equals(IndexFolder, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private void WriteIndex(string repoRoot, RepoIndex index)
    {
        try
        {
            var outputDir = Path.Combine(repoRoot, IndexFolder);
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, IndexFile);
            var json = JsonSerializer.Serialize(index, JsonOptions);
            File.WriteAllText(outputPath, json);

            logger.LogInformation("Written index to {Path}", outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to write index for {Repo}: {Message}", repoRoot, ex.Message);
        }
    }
}

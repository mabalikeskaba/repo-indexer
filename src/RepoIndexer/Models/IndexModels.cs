using System.Text.Json.Serialization;

namespace RepoIndexer.Models;

public class RepoIndex
{
    [JsonPropertyName("repoName")]
    public string RepoName { get; set; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("files")]
    public List<FileIndex> Files { get; set; } = [];
}

public class FileIndex
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("classes")]
    public List<ClassIndex> Classes { get; set; } = [];
}

public class ClassIndex
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("methods")]
    public List<string> Methods { get; set; } = [];
}

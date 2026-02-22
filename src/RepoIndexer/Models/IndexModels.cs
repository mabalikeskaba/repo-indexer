using System.Text.Json.Serialization;

namespace RepoIndexer.Models;

/// <summary>
/// Represents the index of a repository, including its files and classes.
/// </summary>
public class RepoIndex
{
    /// <summary>
    /// The name of the repository.
    /// </summary>
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the index was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// The list of files in the repository.
    /// </summary>
    public List<FileIndex> Files { get; set; } = [];
}

/// <summary>
/// Represents the index of a file, including its classes.
/// </summary>
public class FileIndex
{
    /// <summary>
    /// The relative path of the file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The list of classes in the file.
    /// </summary>
    public List<ClassIndex> Classes { get; set; } = [];
}

/// <summary>
/// Represents the index of a class, including its methods.
/// </summary>
public class ClassIndex
{
    /// <summary>
    /// The name of the class.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The summary of the class.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// The list of methods in the class.
    /// </summary>
    public List<string> Methods { get; set; } = [];
}

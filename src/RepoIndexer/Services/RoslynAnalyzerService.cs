using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using RepoIndexer.Models;
using System.Text.RegularExpressions;

namespace RepoIndexer.Services;

public class RoslynAnalyzerService(ILogger<RoslynAnalyzerService> logger)
{
    public FileIndex? AnalyzeFile(string filePath, string repoRoot)
    {
        try
        {
            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            var classes = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(c => new ClassIndex
                {
                    Name = c.Identifier.Text,
                    Summary = ExtractSummary(c),
                    Methods = c.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Select(m => m.Identifier.Text)
                        .ToList()
                })
                .ToList();

            if (classes.Count == 0)
                return null;

            var relativePath = Path.GetRelativePath(repoRoot, filePath)
                .Replace('\\', '/');

            return new FileIndex
            {
                Path = relativePath,
                Classes = classes
            };
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to analyze {File}: {Message}", filePath, ex.Message);
            return null;
        }
    }

    private static string? ExtractSummary(ClassDeclarationSyntax classDecl)
    {
        var xmlTrivia = classDecl
            .GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (xmlTrivia is null)
            return null;

        var summaryElement = xmlTrivia
            .ChildNodes()
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");

        if (summaryElement is null)
            return null;

        var raw = string.Concat(summaryElement.Content.Select(c => c.ToString()));
        return Regex.Replace(raw.Replace("///", ""), @"\s+", " ").Trim();
    }
}

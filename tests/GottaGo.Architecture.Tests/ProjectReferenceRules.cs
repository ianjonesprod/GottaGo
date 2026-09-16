using System.Xml.Linq;

namespace GottaGo.Architecture.Tests;

/// <summary>
/// Reads the .csproj files themselves. Assembly reflection only sees references the compiler
/// actually emitted, so an unused-but-declared package slips straight past it — which makes
/// reflection useless as a guard against someone *adding* a forbidden dependency.
/// </summary>
internal static class ProjectReferenceRules
{
    /// <summary>
    /// Backslash (ASCII 92). MSBuild always writes Include paths with backslashes, on every
    /// platform. Written numerically because C# resolves \ during lexing, so the obvious
    /// spellings both break the literal.
    /// </summary>
    private const char WindowsSeparator = (char)92;

    internal static string RepoRoot { get; } = FindRepoRoot();

    internal static IReadOnlyList<string> DependenciesOf(string projectRelativePath)
    {
        var csproj = Path.Combine(RepoRoot, projectRelativePath);
        if (!File.Exists(csproj))
        {
            throw new FileNotFoundException($"Could not find project file at {csproj}");
        }

        var document = XDocument.Load(csproj);

        return document.Descendants()
            .Where(e => e.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!.Replace(WindowsSeparator, '/')))
            .ToArray();
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        // .NET 10 scaffolds the XML .slnx format; accept either so this keeps working
        // whichever format the solution is stored in.
        while (directory is not null && !directory.EnumerateFiles("GottaGo.sln*").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Could not locate GottaGo.sln/.slnx above the test output directory.");
    }
}

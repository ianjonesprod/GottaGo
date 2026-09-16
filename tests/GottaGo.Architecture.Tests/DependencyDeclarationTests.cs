using Shouldly;

namespace GottaGo.Architecture.Tests;

/// <summary>
/// The primary layering guard: what each project is ALLOWED to declare as a dependency.
/// These fail the moment someone runs `dotnet add package`, before any leaky code is written.
/// </summary>
public class DependencyDeclarationTests
{
    private const string DomainProject = @"src\api\GottaGo.Domain\GottaGo.Domain.csproj";
    private const string ApplicationProject = @"src\api\GottaGo.Application\GottaGo.Application.csproj";

    /// <summary>Anything that implies persistence, HTTP, or cloud storage.</summary>
    private static readonly string[] ForbiddenInInnerLayers =
    [
        "GottaGo.Infrastructure",
        "GottaGo.Api",
        "Dapper",
        "DbUp",
        "Microsoft.Data.SqlClient",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore.App",
        "Azure.Storage.Blobs",
        "SixLabors.ImageSharp",
    ];

    [Fact]
    public void Domain_declares_no_dependencies_at_all()
    {
        ProjectReferenceRules.DependenciesOf(DomainProject)
            .ShouldBeEmpty("Domain is the innermost layer: entities, value objects, and invariants only.");
    }

    [Fact]
    public void Application_declares_no_persistence_or_web_dependency()
    {
        var declared = ProjectReferenceRules.DependenciesOf(ApplicationProject);

        var offenders = declared
            .Where(d => ForbiddenInInnerLayers.Contains(d, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        offenders.ShouldBeEmpty(
            "Application owns the repository interfaces and must not know how they are implemented. "
            + $"Remove: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Application_depends_only_on_Domain()
    {
        ProjectReferenceRules.DependenciesOf(ApplicationProject)
            .ShouldBe(["GottaGo.Domain"]);
    }
}

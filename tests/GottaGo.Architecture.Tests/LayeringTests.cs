using System.Reflection;
using GottaGo.Application;
using GottaGo.Domain;
using Shouldly;

namespace GottaGo.Architecture.Tests;

/// <summary>
/// Executable versions of the layering rules in CLAUDE.md section 2. A convention written
/// in a document decays; a red build does not.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(IDomainAssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(IApplicationAssemblyMarker).Assembly;

    /// <summary>Names that must never appear in Application's reference graph.</summary>
    private static readonly string[] PersistenceAndWebPrefixes =
    [
        "GottaGo.Infrastructure",
        "Dapper",
        "Microsoft.Data",
        "Microsoft.AspNetCore",
        "Azure.Storage",
    ];

    [Fact]
    public void Domain_depends_on_nothing_but_the_base_class_library()
    {
        var offenders = Domain.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !name.StartsWith("System.") && name != "System" && name != "netstandard")
            .ToArray();

        offenders.ShouldBeEmpty(
            $"Domain must stay dependency-free, but references: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Application_does_not_reference_persistence_or_web_packages()
    {
        var offenders = Application.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => PersistenceAndWebPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        offenders.ShouldBeEmpty(
            "Application owns the repository interfaces; it must not know how they are implemented. "
            + $"Offending references: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Application_never_exposes_a_database_type_in_a_public_signature()
    {
        string[] leakyTypes = ["IDbConnection", "IDbTransaction", "DataTable", "SqlConnection", "IQueryable`1"];

        var leaks = Application.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType),
                        (method, type) => new { method, type })
            .Where(x => leakyTypes.Contains(x.type.Name))
            .Select(x => $"{x.method.DeclaringType!.Name}.{x.method.Name} -> {x.type.Name}")
            .ToArray();

        leaks.ShouldBeEmpty(
            $"Repository methods must return domain objects, not queryables or connections: {string.Join(", ", leaks)}");
    }

    [Fact]
    public void Neither_inner_layer_contains_a_transport_concern()
    {
        var offenders = Domain.GetExportedTypes().Concat(Application.GetExportedTypes())
            .Where(t => t.Name.EndsWith("Dto", StringComparison.Ordinal)
                     || t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();

        offenders.ShouldBeEmpty(
            $"HTTP shapes belong in the Api project, not in Domain or Application: {string.Join(", ", offenders)}");
    }
}

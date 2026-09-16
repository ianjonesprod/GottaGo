namespace GottaGo.Infrastructure.Seed;

/// <summary>The shape of one entry in cleveland-bathrooms.json.</summary>
internal sealed record SeedBathroom
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Venue { get; init; } = "Other";
    public string? AccessNote { get; init; }
    public string? SourceUrl { get; init; }
}

/// <summary>
/// A stable hash for seeding the random number generator.
///
/// String.GetHashCode is randomised per process in .NET, so using it would give a different
/// demo dataset on every run. FNV-1a is stable across processes and machines, which is what
/// makes an end-to-end test able to assert a specific rating.
/// </summary>
internal static class StableHash
{
    public static int Of(string value)
    {
        const uint OffsetBasis = 2166136261;
        const uint Prime = 16777619;

        var hash = OffsetBasis;

        foreach (var c in value)
        {
            hash ^= c;
            hash *= Prime;
        }

        return (int)(hash & 0x7FFFFFFF);
    }
}

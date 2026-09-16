using GottaGo.Domain.Bathrooms;

namespace GottaGo.Application.Bathrooms;

public enum BathroomSort
{
    Distance,
    Rating,
    ReviewCount,
    Name,
}

/// <summary>
/// Everything the map and list views can ask for, as plain data.
///
/// This exists so repositories never hand back a queryable for callers to bolt filters onto.
/// The query is declared here and translated to SQL in one place, which keeps the Application
/// layer ignorant of the database.
/// </summary>
public sealed record BathroomSearchQuery
{
    public string? Keyword { get; init; }

    /// <summary>Map viewport, when the caller is looking at a map. Wins over <see cref="Near"/>.</summary>
    public MapBounds? Bounds { get; init; }

    public GeoPoint? Near { get; init; }

    public double? RadiusMiles { get; init; }

    public VenueKind? Venue { get; init; }

    public BathroomSort Sort { get; init; } = BathroomSort.Rating;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}

/// <summary>A rectangular slice of the map.</summary>
public sealed record MapBounds(double North, double South, double East, double West);

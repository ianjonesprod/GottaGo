namespace GottaGo.Infrastructure.Repositories;

/// <summary>
/// Shared SELECT fragments. Averages are computed on read rather than stored, so every query
/// that returns a bathroom joins the same aggregate sub-select.
/// </summary>
internal static class SqlFragments
{
    internal const string BathroomColumns = """
        b.Id, b.Slug, b.Name, b.Description, b.Street, b.City, b.State, b.PostalCode,
        b.Latitude, b.Longitude, b.Venue, b.AccessNote, b.CreatedByUserId, b.IsSeedData, b.CreatedAtUtc,
        ISNULL(r.ReviewCount, 0)      AS ReviewCount,
        ISNULL(r.AvgSmell, 0)         AS AvgSmell,
        ISNULL(r.AvgCleanliness, 0)   AS AvgCleanliness,
        ISNULL(r.AvgAmenities, 0)     AS AvgAmenities,
        ISNULL(r.AvgAccessibility, 0) AS AvgAccessibility,
        ISNULL(r.AvgAmbience, 0)      AS AvgAmbience
        """;

    internal const string RatingsJoin = """
        LEFT JOIN (
            SELECT BathroomId,
                   COUNT(*)                    AS ReviewCount,
                   AVG(CAST(Smell AS FLOAT))         AS AvgSmell,
                   AVG(CAST(Cleanliness AS FLOAT))   AS AvgCleanliness,
                   AVG(CAST(Amenities AS FLOAT))     AS AvgAmenities,
                   AVG(CAST(Accessibility AS FLOAT)) AS AvgAccessibility,
                   AVG(CAST(Ambience AS FLOAT))      AS AvgAmbience
            FROM dbo.Reviews
            GROUP BY BathroomId
        ) r ON r.BathroomId = b.Id
        """;
}

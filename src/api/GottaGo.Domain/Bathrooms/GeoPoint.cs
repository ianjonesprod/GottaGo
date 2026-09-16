using GottaGo.Domain.Common;

namespace GottaGo.Domain.Bathrooms;

/// <summary>A validated latitude/longitude pair.</summary>
public sealed record GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new DomainException($"Latitude must be between -90 and 90, but was {latitude}.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new DomainException($"Longitude must be between -180 and 180, but was {longitude}.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>
    /// Great-circle distance in miles. Good enough for "how far is this bathroom" and it
    /// needs no database or external service, so it can be unit tested directly.
    /// </summary>
    public double MilesTo(GeoPoint other)
    {
        const double earthRadiusMiles = 3958.7613;

        var lat1 = double.DegreesToRadians(Latitude);
        var lat2 = double.DegreesToRadians(other.Latitude);
        var deltaLat = double.DegreesToRadians(other.Latitude - Latitude);
        var deltaLon = double.DegreesToRadians(other.Longitude - Longitude);

        var a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2))
            + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));

        return earthRadiusMiles * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }
}

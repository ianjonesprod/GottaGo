namespace GottaGo.Domain.Bathrooms;

/// <summary>What sort of place hosts the bathroom. Drives the map pin icon and list filtering.</summary>
public enum VenueKind
{
    Other = 0,
    Library,
    Park,
    Transit,
    Museum,
    Stadium,
    Market,
    Airport,
    Campus,
    RecreationCenter,
}

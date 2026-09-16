using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Photos;
using GottaGo.Domain.Reviews;
using GottaGo.Domain.Users;

namespace GottaGo.Infrastructure.Repositories;

/*
    Flat shapes matching the SELECT column lists, plus the translation back into domain
    objects. Kept together so a schema change and its mapping are edited in one file.
*/

internal sealed class BathroomRow
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Venue { get; init; }
    public string? AccessNote { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public bool IsSeedData { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public int ReviewCount { get; init; }
    public double AvgSmell { get; init; }
    public double AvgCleanliness { get; init; }
    public double AvgAmenities { get; init; }
    public double AvgAccessibility { get; init; }
    public double AvgAmbience { get; init; }

    public Bathroom ToDomain()
    {
        var bathroom = new Bathroom(
            Id,
            Slug,
            Name,
            Description,
            new Address(Street, City, State, PostalCode),
            new GeoPoint(Latitude, Longitude),
            (VenueKind)Venue,
            AccessNote,
            CreatedByUserId,
            IsSeedData,
            new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero));

        bathroom.ApplyRatings(ReviewCount == 0
            ? RatingAverages.None
            : new RatingAverages(ReviewCount, AvgSmell, AvgCleanliness, AvgAmenities, AvgAccessibility, AvgAmbience));

        return bathroom;
    }
}

internal sealed class ReviewRow
{
    public Guid Id { get; init; }
    public Guid BathroomId { get; init; }
    public Guid AuthorId { get; init; }
    public string? Headline { get; init; }
    public string Body { get; init; } = string.Empty;
    public byte Smell { get; init; }
    public byte Cleanliness { get; init; }
    public byte Amenities { get; init; }
    public byte Accessibility { get; init; }
    public byte Ambience { get; init; }
    public DateTime? VisitedOn { get; init; }
    public bool IsSeedData { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public string BathroomName { get; init; } = string.Empty;
    public string BathroomSlug { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;

    public Review ToDomain() => new(
        Id,
        BathroomId,
        AuthorId,
        Headline,
        Body,
        new RatingSet(Smell, Cleanliness, Amenities, Accessibility, Ambience),
        VisitedOn is null ? null : DateOnly.FromDateTime(VisitedOn.Value),
        IsSeedData,
        new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero));
}

internal sealed class PhotoRow
{
    public Guid Id { get; init; }
    public Guid BathroomId { get; init; }
    public string BlobName { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsSeedData { get; init; }

    public Photo ToDomain() => new(Id, BathroomId, BlobName, AltText, SortOrder, IsSeedData);
}

internal sealed class UserRow
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public bool IsSeedUser { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public UserProfile ToDomain() =>
        new(Id, DisplayName, IsSeedUser, new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero));
}

using GottaGo.Domain.Common;
using GottaGo.Domain.Photos;
using GottaGo.Domain.Reviews;

namespace GottaGo.Domain.Bathrooms;

/// <summary>
/// A public bathroom somebody can find on the map and review.
/// </summary>
public sealed class Bathroom
{
    private readonly List<Photo> photos = [];

    public Bathroom(
        Guid id,
        string slug,
        string name,
        string? description,
        Address address,
        GeoPoint location,
        VenueKind venue,
        string? accessNote,
        Guid? createdByUserId,
        bool isSeedData,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A bathroom needs a name people will recognise.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("A bathroom needs a slug for stable URLs and seeding.");
        }

        Id = id;
        Slug = slug;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Address = address;
        Location = location;
        Venue = venue;
        AccessNote = string.IsNullOrWhiteSpace(accessNote) ? null : accessNote.Trim();
        CreatedByUserId = createdByUserId;
        IsSeedData = isSeedData;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    /// <summary>Stable URL key, e.g. "west-side-market". Unique, and the seeder upserts on it.</summary>
    public string Slug { get; }

    public string Name { get; }

    public string? Description { get; }

    public Address Address { get; }

    public GeoPoint Location { get; }

    public VenueKind Venue { get; }

    /// <summary>
    /// Practical caveats like "seasonal", "customers only" or "ask for the key at the counter".
    /// Several genuinely public Cleveland bathrooms are gated this way, and a user who walks
    /// there only to find it shut is worse served than one who was told up front.
    /// </summary>
    public string? AccessNote { get; }

    public Guid? CreatedByUserId { get; }

    public bool IsSeedData { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<Photo> Photos => photos;

    /// <summary>
    /// Averaged review scores. Not persisted: it is computed by the query that loads the
    /// bathroom, because at this dataset size an average is cheaper than keeping a
    /// denormalised copy correct on every write.
    /// </summary>
    public RatingAverages Ratings { get; private set; } = RatingAverages.None;

    public static Bathroom Create(
        string name,
        string? description,
        Address address,
        GeoPoint location,
        VenueKind venue,
        string? accessNote,
        Guid? createdByUserId,
        DateTimeOffset now,
        bool isSeedData = false) =>
        new(Guid.NewGuid(), Common.Slug.From(name), name, description, address, location, venue,
            accessNote, createdByUserId, isSeedData, now);

    public void ApplyRatings(RatingAverages averages) => Ratings = averages;

    public void AttachPhoto(Photo photo)
    {
        if (photo.BathroomId != Id)
        {
            throw new DomainException("That photo belongs to a different bathroom.");
        }

        photos.Add(photo);
    }
}

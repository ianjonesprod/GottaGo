using GottaGo.Domain.Common;

namespace GottaGo.Domain.Photos;

/// <summary>
/// A gallery image for a bathroom.
///
/// Alt text is required by the constructor, so a photo with no alt text cannot be created at
/// all. That makes accessibility a property of the type rather than something a reviewer has
/// to remember to check.
/// </summary>
public sealed class Photo
{
    public Photo(Guid id, Guid bathroomId, string blobName, string altText, int sortOrder, bool isSeedData)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new DomainException("A photo needs a storage name.");
        }

        if (string.IsNullOrWhiteSpace(altText))
        {
            throw new DomainException(
                "A photo needs alt text describing it. Screen reader users cannot see the image.");
        }

        Id = id;
        BathroomId = bathroomId;
        BlobName = blobName;
        AltText = altText.Trim();
        SortOrder = sortOrder;
        IsSeedData = isSeedData;
    }

    public Guid Id { get; }

    public Guid BathroomId { get; }

    /// <summary>Key within the blob container. The public URL is built by the storage adapter.</summary>
    public string BlobName { get; }

    public string AltText { get; }

    public int SortOrder { get; }

    public bool IsSeedData { get; }

    public static Photo Create(Guid bathroomId, string blobName, string altText, int sortOrder, bool isSeedData = false) =>
        new(Guid.NewGuid(), bathroomId, blobName, altText, sortOrder, isSeedData);
}

using GottaGo.Application.Abstractions;

namespace GottaGo.Infrastructure.Storage;

/// <summary>
/// Serves the seeded gallery images, which ship as static files with the frontend.
///
/// Uploading is deliberately not implemented here. User-supplied photos need a real blob
/// store, size and type limits, and re-encoding to strip the GPS coordinates a phone writes
/// into every picture. That arrives with the upload feature; until then nothing calls
/// <see cref="SaveAsync"/>, and failing loudly is better than silently writing somewhere it
/// should not.
/// </summary>
internal sealed class StaticAssetPhotoStorage : IPhotoStorage
{
    private const string AssetRoot = "/assets/demo";

    public string GetUrl(string blobName) => $"{AssetRoot}/{blobName.TrimStart('/')}";

    public Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Photo upload is not available yet. It needs a blob store plus EXIF stripping, "
            + "which arrives with the upload feature.");

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Photo upload is not available yet, so there is nothing to delete.");
}

namespace GottaGo.Application.Abstractions;

/// <summary>
/// Somewhere to put gallery images. Deliberately says nothing about Azure, disks or
/// containers, so swapping the backing store is a change in one adapter.
/// </summary>
public interface IPhotoStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>The URL a browser should load this image from.</summary>
    string GetUrl(string blobName);
}

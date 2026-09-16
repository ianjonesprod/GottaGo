using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Application.Bathrooms;

/// <summary>
/// How the application reads and writes bathrooms. Implemented in the infrastructure layer.
///
/// Every method returns domain objects or plain records. Nothing here exposes a connection,
/// a transaction or a queryable, so deleting the implementation leaves this layer compiling.
/// </summary>
public interface IBathroomRepository
{
    Task<Bathroom?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Bathroom?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Paged<Bathroom>> SearchAsync(BathroomSearchQuery query, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task AddAsync(Bathroom bathroom, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a bathroom and its first review together. One call rather than two so the
    /// transaction stays inside the repository and callers cannot half-commit a submission.
    /// </summary>
    Task AddWithFirstReviewAsync(Bathroom bathroom, Review firstReview, CancellationToken cancellationToken);
}

using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Application.Reviews;

public sealed record ReviewFeedQuery(string? Keyword = null, int Page = 1, int PageSize = 25);

/// <summary>A review together with the bathroom it is about, for the list view.</summary>
public sealed record ReviewWithContext(Review Review, Guid BathroomId, string BathroomName, string BathroomSlug, string AuthorName);

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Paged<ReviewWithContext>> ListForBathroomAsync(Guid bathroomId, int page, int pageSize, CancellationToken cancellationToken);

    Task<Paged<ReviewWithContext>> ListRecentAsync(ReviewFeedQuery query, CancellationToken cancellationToken);

    Task<Review?> FindByAuthorAsync(Guid bathroomId, Guid authorId, CancellationToken cancellationToken);

    Task AddAsync(Review review, CancellationToken cancellationToken);

    Task ReplaceAsync(Review review, CancellationToken cancellationToken);

    Task DeleteAsync(Guid reviewId, CancellationToken cancellationToken);
}

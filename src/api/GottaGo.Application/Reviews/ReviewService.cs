using GottaGo.Application.Abstractions;
using GottaGo.Application.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Application.Reviews;

public sealed record SubmitReviewCommand(
    Guid BathroomId,
    Guid AuthorId,
    string? Headline,
    string Body,
    RatingSet Scores,
    DateOnly? VisitedOn);

public sealed class ReviewService(
    IReviewRepository reviews,
    IBathroomRepository bathrooms,
    IClock clock)
{
    public Task<Paged<ReviewWithContext>> ListForBathroomAsync(
        Guid bathroomId, int page, int pageSize, CancellationToken cancellationToken) =>
        reviews.ListForBathroomAsync(bathroomId, page, pageSize, cancellationToken);

    public Task<Paged<ReviewWithContext>> ListRecentAsync(ReviewFeedQuery query, CancellationToken cancellationToken) =>
        reviews.ListRecentAsync(query, cancellationToken);

    /// <summary>
    /// Records a review. Somebody reviewing a bathroom they have already reviewed updates
    /// their existing review rather than adding a second one, which keeps one person from
    /// moving an average on their own.
    /// </summary>
    public async Task<Review> SubmitAsync(SubmitReviewCommand command, CancellationToken cancellationToken)
    {
        var bathroom = await bathrooms.GetByIdAsync(command.BathroomId, cancellationToken)
            ?? throw new NotFoundException($"Bathroom '{command.BathroomId}'");

        var existing = await reviews.FindByAuthorAsync(bathroom.Id, command.AuthorId, cancellationToken);
        var now = clock.UtcNow;

        if (existing is null)
        {
            var review = Review.Create(
                bathroom.Id, command.AuthorId, command.Headline, command.Body,
                command.Scores, command.VisitedOn, now);

            await reviews.AddAsync(review, cancellationToken);

            return review;
        }

        var updated = new Review(
            existing.Id,
            existing.BathroomId,
            existing.AuthorId,
            command.Headline,
            command.Body,
            command.Scores,
            command.VisitedOn,
            existing.IsSeedData,
            existing.CreatedAt);

        await reviews.ReplaceAsync(updated, cancellationToken);

        return updated;
    }

    public async Task DeleteAsync(Guid reviewId, Guid requestedBy, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(reviewId, cancellationToken)
            ?? throw new NotFoundException($"Review '{reviewId}'");

        if (review.AuthorId != requestedBy)
        {
            throw new ForbiddenException("You can only delete your own review.");
        }

        await reviews.DeleteAsync(reviewId, cancellationToken);
    }
}

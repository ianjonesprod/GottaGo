using GottaGo.Application.Abstractions;
using GottaGo.Application.Reviews;
using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Application.Bathrooms;

public sealed record CreateBathroomRequest(
    string Name,
    string? Description,
    Address Address,
    GeoPoint Location,
    VenueKind Venue,
    string? AccessNote,
    RatingSet? FirstReviewScores,
    string? FirstReviewBody);

public sealed class BathroomService(
    IBathroomRepository bathrooms,
    IClock clock)
{
    public Task<Paged<Bathroom>> SearchAsync(BathroomSearchQuery query, CancellationToken cancellationToken) =>
        bathrooms.SearchAsync(query, cancellationToken);

    public async Task<Bathroom> GetAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        // The detail panel is reached both from a shareable slug URL and from a map marker
        // holding an id, so accept either.
        var bathroom = Guid.TryParse(idOrSlug, out var id)
            ? await bathrooms.GetByIdAsync(id, cancellationToken)
            : await bathrooms.GetBySlugAsync(idOrSlug, cancellationToken);

        return bathroom ?? throw new NotFoundException($"Bathroom '{idOrSlug}'");
    }

    public async Task<Bathroom> CreateAsync(CreateBathroomRequest request, Guid authorId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var slug = await UniqueSlugAsync(request.Name, cancellationToken);

        var bathroom = new Bathroom(
            Guid.NewGuid(),
            slug,
            request.Name,
            request.Description,
            request.Address,
            request.Location,
            request.Venue,
            request.AccessNote,
            authorId,
            isSeedData: false,
            now);

        if (request.FirstReviewScores is { } scores && !string.IsNullOrWhiteSpace(request.FirstReviewBody))
        {
            var review = Review.Create(bathroom.Id, authorId, null, request.FirstReviewBody, scores, null, now);

            await bathrooms.AddWithFirstReviewAsync(bathroom, review, cancellationToken);
            bathroom.ApplyRatings(RatingAverages.From([review]));
        }
        else
        {
            await bathrooms.AddAsync(bathroom, cancellationToken);
        }

        return bathroom;
    }

    /// <summary>
    /// Two bathrooms can legitimately share a name - Cleveland has several library branches -
    /// so a clashing slug gets a numeric suffix rather than failing the submission.
    /// </summary>
    private async Task<string> UniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = Slug.From(name);

        if (!await bathrooms.SlugExistsAsync(baseSlug, cancellationToken))
        {
            return baseSlug;
        }

        for (var suffix = 2; suffix < 100; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";

            if (!await bathrooms.SlugExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }
}

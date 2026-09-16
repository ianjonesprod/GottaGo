using GottaGo.Application.HighScores;
using GottaGo.Application.Reviews;
using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Api.Common;

/*
    The wire contract.

    These shapes are deliberately separate from the domain objects. If they were the same
    types, renaming a domain property would silently change the JSON every client depends on.
    Mapping here means a rename is a compile error in one file instead of a broken frontend.
*/

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record AddressDto(string Street, string City, string State, string PostalCode, string SingleLine);

public sealed record LocationDto(double Latitude, double Longitude);

public sealed record RatingDto(
    double Overall,
    int ReviewCount,
    double Smell,
    double Cleanliness,
    double Amenities,
    double Accessibility,
    double Ambience);

public sealed record ScoresDto(int Smell, int Cleanliness, int Amenities, int Accessibility, int Ambience);

public sealed record PhotoDto(Guid Id, string Url, string AltText);

public sealed record BathroomSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    AddressDto Address,
    LocationDto Location,
    string Venue,
    string? AccessNote,
    RatingDto Rating,
    PhotoDto? PrimaryPhoto,
    bool IsSeedData);

public sealed record BathroomDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    AddressDto Address,
    LocationDto Location,
    string Venue,
    string? AccessNote,
    RatingDto Rating,
    IReadOnlyList<PhotoDto> Photos,
    bool IsSeedData);

public sealed record ReviewDto(
    Guid Id,
    Guid BathroomId,
    string BathroomName,
    string BathroomSlug,
    string AuthorName,
    string? Headline,
    string Body,
    ScoresDto Scores,
    double Overall,
    DateOnly? VisitedOn,
    DateTimeOffset CreatedAt,
    bool IsSeedData);

public sealed record HighScoreEntryDto(
    int Rank,
    Guid BathroomId,
    string Slug,
    string Name,
    string City,
    RatingDto Rating,
    double RankingScore,
    bool IsSeedData);

/// <summary>Turns domain objects into the wire shapes above.</summary>
internal static class DtoMapper
{
    internal static PagedResponse<TOut> ToResponse<TIn, TOut>(this Paged<TIn> page, Func<TIn, TOut> map) =>
        new([.. page.Items.Select(map)], page.Page, page.PageSize, page.TotalCount, page.TotalPages);

    internal static AddressDto ToDto(this Address address) =>
        new(address.Street, address.City, address.State, address.PostalCode, address.SingleLine);

    internal static LocationDto ToDto(this GeoPoint point) => new(point.Latitude, point.Longitude);

    internal static RatingDto ToDto(this RatingAverages ratings) => new(
        Math.Round(ratings.Overall, 2),
        ratings.ReviewCount,
        Math.Round(ratings.Smell, 2),
        Math.Round(ratings.Cleanliness, 2),
        Math.Round(ratings.Amenities, 2),
        Math.Round(ratings.Accessibility, 2),
        Math.Round(ratings.Ambience, 2));

    internal static ScoresDto ToDto(this RatingSet scores) =>
        new(scores.Smell, scores.Cleanliness, scores.Amenities, scores.Accessibility, scores.Ambience);

    internal static BathroomSummaryDto ToSummaryDto(this Bathroom bathroom, Func<string, string> photoUrl)
    {
        var primary = bathroom.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

        return new BathroomSummaryDto(
            bathroom.Id,
            bathroom.Slug,
            bathroom.Name,
            bathroom.Description,
            bathroom.Address.ToDto(),
            bathroom.Location.ToDto(),
            bathroom.Venue.ToString(),
            bathroom.AccessNote,
            bathroom.Ratings.ToDto(),
            primary is null ? null : new PhotoDto(primary.Id, photoUrl(primary.BlobName), primary.AltText),
            bathroom.IsSeedData);
    }

    internal static BathroomDetailDto ToDetailDto(this Bathroom bathroom, Func<string, string> photoUrl) => new(
        bathroom.Id,
        bathroom.Slug,
        bathroom.Name,
        bathroom.Description,
        bathroom.Address.ToDto(),
        bathroom.Location.ToDto(),
        bathroom.Venue.ToString(),
        bathroom.AccessNote,
        bathroom.Ratings.ToDto(),
        [.. bathroom.Photos.OrderBy(p => p.SortOrder)
            .Select(p => new PhotoDto(p.Id, photoUrl(p.BlobName), p.AltText))],
        bathroom.IsSeedData);

    internal static ReviewDto ToDto(this ReviewWithContext context)
    {
        var review = context.Review;

        return new ReviewDto(
            review.Id,
            context.BathroomId,
            context.BathroomName,
            context.BathroomSlug,
            context.AuthorName,
            review.Headline,
            review.Body,
            review.Scores.ToDto(),
            Math.Round(review.Scores.Overall, 2),
            review.VisitedOn,
            review.CreatedAt,
            review.IsSeedData);
    }

    internal static HighScoreEntryDto ToDto(this HighScoreEntry entry) => new(
        entry.Rank,
        entry.BathroomId,
        entry.Slug,
        entry.Name,
        entry.City,
        entry.Ratings.ToDto(),
        Math.Round(entry.RankingScore, 3),
        entry.IsSeedData);
}

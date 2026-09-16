using GottaGo.Domain.Common;

namespace GottaGo.Domain.Reviews;

/// <summary>One person's scored opinion of one bathroom.</summary>
public sealed class Review
{
    public const int MaxHeadlineLength = 120;
    public const int MaxBodyLength = 2000;

    public Review(
        Guid id,
        Guid bathroomId,
        Guid authorId,
        string? headline,
        string body,
        RatingSet scores,
        DateOnly? visitedOn,
        bool isSeedData,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("A review needs some text.");
        }

        if (body.Length > MaxBodyLength)
        {
            throw new DomainException($"A review body cannot exceed {MaxBodyLength} characters.");
        }

        if (headline is { Length: > MaxHeadlineLength })
        {
            throw new DomainException($"A review headline cannot exceed {MaxHeadlineLength} characters.");
        }

        Id = id;
        BathroomId = bathroomId;
        AuthorId = authorId;
        Headline = string.IsNullOrWhiteSpace(headline) ? null : headline.Trim();
        Body = body.Trim();
        Scores = scores;
        VisitedOn = visitedOn;
        IsSeedData = isSeedData;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid BathroomId { get; }

    public Guid AuthorId { get; }

    public string? Headline { get; }

    public string Body { get; }

    public RatingSet Scores { get; }

    public DateOnly? VisitedOn { get; }

    public bool IsSeedData { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Review Create(
        Guid bathroomId,
        Guid authorId,
        string? headline,
        string body,
        RatingSet scores,
        DateOnly? visitedOn,
        DateTimeOffset now,
        bool isSeedData = false) =>
        new(Guid.NewGuid(), bathroomId, authorId, headline, body, scores, visitedOn, isSeedData, now);
}

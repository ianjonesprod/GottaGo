using GottaGo.Domain.Common;

namespace GottaGo.Domain.Reviews;

/// <summary>
/// A complete set of scores for one review. Every dimension is required: a partially rated
/// review would quietly skew the averages, so the type refuses to exist in that state.
/// </summary>
public sealed record RatingSet
{
    public const int MinScore = 1;
    public const int MaxScore = 5;

    public RatingSet(int smell, int cleanliness, int amenities, int accessibility, int ambience)
    {
        Smell = Validated(smell, nameof(smell));
        Cleanliness = Validated(cleanliness, nameof(cleanliness));
        Amenities = Validated(amenities, nameof(amenities));
        Accessibility = Validated(accessibility, nameof(accessibility));
        Ambience = Validated(ambience, nameof(ambience));
    }

    public int Smell { get; }

    public int Cleanliness { get; }

    public int Amenities { get; }

    public int Accessibility { get; }

    public int Ambience { get; }

    /// <summary>The unweighted mean of the five dimensions.</summary>
    public double Overall => (Smell + Cleanliness + Amenities + Accessibility + Ambience) / 5.0;

    public int this[RatingDimension dimension] => dimension switch
    {
        RatingDimension.Smell => Smell,
        RatingDimension.Cleanliness => Cleanliness,
        RatingDimension.Amenities => Amenities,
        RatingDimension.Accessibility => Accessibility,
        RatingDimension.Ambience => Ambience,
        _ => throw new DomainException($"Unknown rating dimension '{dimension}'."),
    };

    private static int Validated(int score, string dimension) =>
        score is >= MinScore and <= MaxScore
            ? score
            : throw new DomainException($"{dimension} must be between {MinScore} and {MaxScore}, but was {score}.");
}

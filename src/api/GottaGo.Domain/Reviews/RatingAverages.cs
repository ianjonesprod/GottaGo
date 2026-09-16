namespace GottaGo.Domain.Reviews;

/// <summary>
/// Averaged scores for a bathroom, plus how many reviews produced them.
/// The count matters as much as the average: four stars from fifty people is a different
/// claim from four stars from one, and the High Scores ranking depends on the difference.
/// </summary>
public sealed record RatingAverages(
    int ReviewCount,
    double Smell,
    double Cleanliness,
    double Amenities,
    double Accessibility,
    double Ambience)
{
    public static readonly RatingAverages None = new(0, 0, 0, 0, 0, 0);

    public double Overall => ReviewCount == 0
        ? 0
        : (Smell + Cleanliness + Amenities + Accessibility + Ambience) / 5.0;

    public double this[RatingDimension dimension] => dimension switch
    {
        RatingDimension.Smell => Smell,
        RatingDimension.Cleanliness => Cleanliness,
        RatingDimension.Amenities => Amenities,
        RatingDimension.Accessibility => Accessibility,
        _ => Ambience,
    };

    /// <summary>Averages a set of reviews. Returns <see cref="None"/> when there are no reviews.</summary>
    public static RatingAverages From(IReadOnlyCollection<Review> reviews)
    {
        if (reviews.Count == 0)
        {
            return None;
        }

        return new RatingAverages(
            reviews.Count,
            reviews.Average(r => r.Scores.Smell),
            reviews.Average(r => r.Scores.Cleanliness),
            reviews.Average(r => r.Scores.Amenities),
            reviews.Average(r => r.Scores.Accessibility),
            reviews.Average(r => r.Scores.Ambience));
    }
}

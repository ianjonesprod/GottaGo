using GottaGo.Domain.Reviews;

namespace GottaGo.Application.HighScores;

/// <summary>
/// Turns a bathroom's average score into a ranking score.
///
/// A raw average ranks badly: a single five-star review would beat a bathroom that forty
/// people rated 4.8. So the average is pulled toward the global mean by a pretend set of
/// <see cref="PriorReviewCount"/> average reviews. The more real reviews a bathroom has, the
/// less that prior matters, which is the behaviour we want - confidence should have to be
/// earned.
/// </summary>
public static class RankingPolicy
{
    /// <summary>How many imaginary average reviews every bathroom starts with.</summary>
    public const double PriorReviewCount = 3.0;

    /// <summary>The score those imaginary reviews carry: the middle of a 1-5 scale, rounded up.</summary>
    public const double GlobalMeanScore = 3.5;

    public static double Score(RatingAverages averages)
    {
        if (averages.ReviewCount == 0)
        {
            return 0;
        }

        var v = averages.ReviewCount;
        var weightOfRealReviews = v / (v + PriorReviewCount);
        var weightOfPrior = PriorReviewCount / (v + PriorReviewCount);

        return (weightOfRealReviews * averages.Overall) + (weightOfPrior * GlobalMeanScore);
    }
}

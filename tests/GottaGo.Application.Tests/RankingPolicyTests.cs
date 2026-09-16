using GottaGo.Application.HighScores;
using GottaGo.Domain.Reviews;
using Shouldly;

namespace GottaGo.Application.Tests;

/// <summary>
/// Pins the leaderboard's behaviour. These are the assertions that stop someone "simplifying"
/// the ranking back to a raw average later.
/// </summary>
public class RankingPolicyTests
{
    private static RatingAverages Averages(int reviewCount, double everyDimension) =>
        new(reviewCount, everyDimension, everyDimension, everyDimension, everyDimension, everyDimension);

    [Fact]
    public void A_bathroom_with_no_reviews_scores_zero() =>
        RankingPolicy.Score(RatingAverages.None).ShouldBe(0);

    [Fact]
    public void Many_good_reviews_beat_one_perfect_review()
    {
        var oneFiveStar = RankingPolicy.Score(Averages(reviewCount: 1, everyDimension: 5.0));
        var fortyNearPerfect = RankingPolicy.Score(Averages(reviewCount: 40, everyDimension: 4.8));

        fortyNearPerfect.ShouldBeGreaterThan(oneFiveStar);
    }

    [Fact]
    public void Confidence_grows_with_review_count()
    {
        var few = RankingPolicy.Score(Averages(reviewCount: 2, everyDimension: 5.0));
        var many = RankingPolicy.Score(Averages(reviewCount: 200, everyDimension: 5.0));

        many.ShouldBeGreaterThan(few);
        many.ShouldBeLessThanOrEqualTo(5.0);
    }

    [Fact]
    public void A_single_review_sits_between_its_own_score_and_the_global_mean()
    {
        var score = RankingPolicy.Score(Averages(reviewCount: 1, everyDimension: 5.0));

        score.ShouldBeGreaterThan(RankingPolicy.GlobalMeanScore);
        score.ShouldBeLessThan(5.0);
    }

    [Fact]
    public void A_terrible_bathroom_is_dragged_up_toward_the_mean_when_barely_reviewed()
    {
        var score = RankingPolicy.Score(Averages(reviewCount: 1, everyDimension: 1.0));

        score.ShouldBeGreaterThan(1.0);
        score.ShouldBeLessThan(RankingPolicy.GlobalMeanScore);
    }
}

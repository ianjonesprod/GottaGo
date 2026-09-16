using GottaGo.Domain.Reviews;
using Shouldly;

namespace GottaGo.Application.Tests;

public class RatingAveragesTests
{
    private static Review ReviewScoring(int everyDimension) => Review.Create(
        bathroomId: Guid.NewGuid(),
        authorId: Guid.NewGuid(),
        headline: null,
        body: "Fine.",
        scores: new RatingSet(everyDimension, everyDimension, everyDimension, everyDimension, everyDimension),
        visitedOn: null,
        now: DateTimeOffset.UnixEpoch);

    [Fact]
    public void No_reviews_means_no_rating() =>
        RatingAverages.From([]).ShouldBe(RatingAverages.None);

    [Fact]
    public void Averages_across_reviews()
    {
        var averages = RatingAverages.From([ReviewScoring(2), ReviewScoring(4)]);

        averages.ReviewCount.ShouldBe(2);
        averages.Cleanliness.ShouldBe(3.0);
        averages.Overall.ShouldBe(3.0);
    }
}

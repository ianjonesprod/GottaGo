using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;
using Shouldly;

namespace GottaGo.Domain.Tests;

public class RatingSetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rejects_scores_outside_one_to_five(int badScore)
    {
        var create = () => new RatingSet(badScore, 3, 3, 3, 3);

        create.ShouldThrow<DomainException>().Message.ShouldContain("between 1 and 5");
    }

    [Fact]
    public void Averages_the_five_dimensions()
    {
        var scores = new RatingSet(smell: 1, cleanliness: 2, amenities: 3, accessibility: 4, ambience: 5);

        scores.Overall.ShouldBe(3.0);
    }

    [Fact]
    public void Exposes_each_dimension_by_name()
    {
        var scores = new RatingSet(smell: 1, cleanliness: 2, amenities: 3, accessibility: 4, ambience: 5);

        scores[RatingDimension.Smell].ShouldBe(1);
        scores[RatingDimension.Cleanliness].ShouldBe(2);
        scores[RatingDimension.Amenities].ShouldBe(3);
        scores[RatingDimension.Accessibility].ShouldBe(4);
        scores[RatingDimension.Ambience].ShouldBe(5);
    }
}

using GottaGo.Domain.Common;
using Shouldly;

namespace GottaGo.Domain.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("West Side Market", "west-side-market")]
    [InlineData("Rock & Roll Hall of Fame", "rock-roll-hall-of-fame")]
    [InlineData("  Public Square  ", "public-square")]
    [InlineData("Edgewater Park - Beach House", "edgewater-park-beach-house")]
    public void Produces_stable_url_safe_keys(string name, string expected) =>
        Slug.From(name).ShouldBe(expected);

    [Fact]
    public void Is_repeatable_because_the_seeder_upserts_on_it() =>
        Slug.From("Tower City Center").ShouldBe(Slug.From("tower city center"));
}

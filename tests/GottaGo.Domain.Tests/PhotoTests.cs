using GottaGo.Domain.Common;
using GottaGo.Domain.Photos;
using Shouldly;

namespace GottaGo.Domain.Tests;

public class PhotoTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cannot_be_created_without_alt_text(string? missingAltText)
    {
        var create = () => Photo.Create(Guid.NewGuid(), "photo.webp", missingAltText!, sortOrder: 0);

        create.ShouldThrow<DomainException>().Message.ShouldContain("alt text");
    }

    [Fact]
    public void Keeps_alt_text_trimmed()
    {
        var photo = Photo.Create(Guid.NewGuid(), "photo.webp", "  Tiled restroom with a wide stall.  ", 0);

        photo.AltText.ShouldBe("Tiled restroom with a wide stall.");
    }
}

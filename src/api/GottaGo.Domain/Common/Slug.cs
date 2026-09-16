using System.Text;
using System.Text.RegularExpressions;

namespace GottaGo.Domain.Common;

/// <summary>
/// Converts a bathroom name into a stable, URL-safe key such as "west-side-market".
/// Slugs are the seeder's idempotency key, so the same name must always produce the same slug.
/// </summary>
public static partial class Slug
{
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Cannot build a slug from an empty name.");
        }

        var lowered = value.Trim().ToLowerInvariant();
        var separated = NonAlphanumeric().Replace(lowered, "-");
        var collapsed = RepeatedDashes().Replace(separated, "-");

        return collapsed.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedDashes();
}

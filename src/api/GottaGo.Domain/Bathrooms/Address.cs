namespace GottaGo.Domain.Bathrooms;

/// <summary>A postal address. Only the city and state are required to be useful on a map.</summary>
public sealed record Address(string Street, string City, string State, string PostalCode)
{
    public string SingleLine => string.Join(", ",
        new[] { Street, City, $"{State} {PostalCode}".Trim() }.Where(p => !string.IsNullOrWhiteSpace(p)));
}

using System.Data;
using Dapper;
using GottaGo.Application.Bathrooms;
using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;
using GottaGo.Infrastructure.Db;
using Microsoft.Data.SqlClient;

namespace GottaGo.Infrastructure.Repositories;

internal sealed class BathroomRepository(ISqlConnectionFactory connections) : IBathroomRepository
{
    private const int MaxPageSize = 200;

    private const string InsertSql = """
        INSERT INTO dbo.Bathrooms
            (Id, Slug, Name, Description, Street, City, State, PostalCode,
             Latitude, Longitude, Venue, AccessNote, CreatedByUserId, IsSeedData, CreatedAtUtc)
        VALUES
            (@Id, @Slug, @Name, @Description, @Street, @City, @State, @PostalCode,
             @Latitude, @Longitude, @Venue, @AccessNote, @CreatedByUserId, @IsSeedData, @CreatedAtUtc)
        """;

    public Task<Bathroom?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetSingleAsync("b.Id = @Key", id, cancellationToken);

    public Task<Bathroom?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        GetSingleAsync("b.Slug = @Key", slug, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM dbo.Bathrooms WHERE Slug = @Slug",
            new { Slug = slug },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<Paged<Bathroom>> SearchAsync(BathroomSearchQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var filters = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            filters.Add("(b.Name LIKE @Keyword OR b.Description LIKE @Keyword OR b.City LIKE @Keyword OR b.Street LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.Venue is { } venue)
        {
            filters.Add("b.Venue = @Venue");
            parameters.Add("Venue", (int)venue);
        }

        AddLocationFilter(query, filters, parameters);

        var where = filters.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", filters)}";

        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var sql = $"""
            SELECT COUNT(1) FROM dbo.Bathrooms b {where};

            SELECT {SqlFragments.BathroomColumns}
            FROM dbo.Bathrooms b
            {SqlFragments.RatingsJoin}
            {where}
            ORDER BY {OrderByFor(query.Sort)}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        using var connection = await connections.OpenAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<BathroomRow>();

        var bathrooms = rows.Select(r => r.ToDomain()).ToList();

        // Distance sorting needs the real great-circle figure, not the bounding box that
        // narrowed the query.
        if (query.Sort == BathroomSort.Distance && query.Near is { } origin)
        {
            bathrooms = [.. bathrooms.OrderBy(b => b.Location.MilesTo(origin))];
        }

        return new Paged<Bathroom>(bathrooms, page, pageSize, total);
    }

    public async Task AddAsync(Bathroom bathroom, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await InsertBathroomAsync(connection, bathroom, transaction: null, cancellationToken);
    }

    public async Task AddWithFirstReviewAsync(Bathroom bathroom, Review firstReview, CancellationToken cancellationToken)
    {
        using var connection = (SqlConnection)await connections.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            await InsertBathroomAsync(connection, bathroom, transaction, cancellationToken);
            await ReviewRepository.InsertReviewAsync(connection, firstReview, transaction, cancellationToken);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// A map viewport is what the user can actually see, so it wins when a radius is also
    /// supplied. The radius case pre-filters with a cheap bounding box and leaves exact
    /// distance to the sort.
    /// </summary>
    private static void AddLocationFilter(BathroomSearchQuery query, List<string> filters, DynamicParameters parameters)
    {
        const string BoxFilter = "b.Latitude BETWEEN @South AND @North AND b.Longitude BETWEEN @West AND @East";

        if (query.Bounds is { } bounds)
        {
            filters.Add(BoxFilter);
            parameters.Add("South", bounds.South);
            parameters.Add("North", bounds.North);
            parameters.Add("West", bounds.West);
            parameters.Add("East", bounds.East);
            return;
        }

        if (query.Near is not { } near || query.RadiusMiles is not { } radius)
        {
            return;
        }

        var latDelta = radius / 69.0;
        var lngDelta = radius / (69.0 * Math.Max(Math.Cos(double.DegreesToRadians(near.Latitude)), 0.01));

        filters.Add(BoxFilter);
        parameters.Add("South", near.Latitude - latDelta);
        parameters.Add("North", near.Latitude + latDelta);
        parameters.Add("West", near.Longitude - lngDelta);
        parameters.Add("East", near.Longitude + lngDelta);
    }

    private static string OrderByFor(BathroomSort sort) => sort switch
    {
        BathroomSort.Name => "b.Name ASC",
        BathroomSort.ReviewCount => "ISNULL(r.ReviewCount, 0) DESC, b.Name ASC",
        BathroomSort.Rating =>
            "ISNULL(r.AvgSmell + r.AvgCleanliness + r.AvgAmenities + r.AvgAccessibility + r.AvgAmbience, 0) DESC, b.Name ASC",
        _ => "b.Name ASC",
    };

    private static Task InsertBathroomAsync(
        IDbConnection connection,
        Bathroom bathroom,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new
            {
                bathroom.Id,
                bathroom.Slug,
                bathroom.Name,
                bathroom.Description,
                bathroom.Address.Street,
                bathroom.Address.City,
                bathroom.Address.State,
                bathroom.Address.PostalCode,
                bathroom.Location.Latitude,
                bathroom.Location.Longitude,
                Venue = (int)bathroom.Venue,
                bathroom.AccessNote,
                bathroom.CreatedByUserId,
                bathroom.IsSeedData,
                CreatedAtUtc = bathroom.CreatedAt.UtcDateTime,
            },
            transaction,
            cancellationToken: cancellationToken));

    private async Task<Bathroom?> GetSingleAsync(string predicate, object key, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT {SqlFragments.BathroomColumns}
            FROM dbo.Bathrooms b
            {SqlFragments.RatingsJoin}
            WHERE {predicate};

            SELECT p.Id, p.BathroomId, p.BlobName, p.AltText, p.SortOrder, p.IsSeedData
            FROM dbo.BathroomPhotos p
            INNER JOIN dbo.Bathrooms b ON b.Id = p.BathroomId
            WHERE {predicate}
            ORDER BY p.SortOrder;
            """;

        using var connection = await connections.OpenAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));

        var row = await results.ReadSingleOrDefaultAsync<BathroomRow>();
        if (row is null)
        {
            return null;
        }

        var bathroom = row.ToDomain();

        foreach (var photo in await results.ReadAsync<PhotoRow>())
        {
            bathroom.AttachPhoto(photo.ToDomain());
        }

        return bathroom;
    }
}

using System.Data;
using Dapper;
using GottaGo.Application.Reviews;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;
using GottaGo.Infrastructure.Db;

namespace GottaGo.Infrastructure.Repositories;

internal sealed class ReviewRepository(ISqlConnectionFactory connections) : IReviewRepository
{
    private const int MaxPageSize = 100;

    private const string ReviewColumns = """
        v.Id, v.BathroomId, v.AuthorId, v.Headline, v.Body,
        v.Smell, v.Cleanliness, v.Amenities, v.Accessibility, v.Ambience,
        v.VisitedOn, v.IsSeedData, v.CreatedAtUtc,
        b.Name AS BathroomName, b.Slug AS BathroomSlug, u.DisplayName AS AuthorName
        """;

    private const string ReviewJoins = """
        FROM dbo.Reviews v
        INNER JOIN dbo.Bathrooms b ON b.Id = v.BathroomId
        INNER JOIN dbo.Users u ON u.Id = v.AuthorId
        """;

    internal const string InsertSql = """
        INSERT INTO dbo.Reviews
            (Id, BathroomId, AuthorId, Headline, Body,
             Smell, Cleanliness, Amenities, Accessibility, Ambience,
             VisitedOn, IsSeedData, CreatedAtUtc)
        VALUES
            (@Id, @BathroomId, @AuthorId, @Headline, @Body,
             @Smell, @Cleanliness, @Amenities, @Accessibility, @Ambience,
             @VisitedOn, @IsSeedData, @CreatedAtUtc)
        """;

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ReviewRow>(new CommandDefinition(
            $"SELECT {ReviewColumns} {ReviewJoins} WHERE v.Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<Review?> FindByAuthorAsync(Guid bathroomId, Guid authorId, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ReviewRow>(new CommandDefinition(
            $"SELECT {ReviewColumns} {ReviewJoins} WHERE v.BathroomId = @BathroomId AND v.AuthorId = @AuthorId",
            new { BathroomId = bathroomId, AuthorId = authorId },
            cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public Task<Paged<ReviewWithContext>> ListForBathroomAsync(
        Guid bathroomId, int page, int pageSize, CancellationToken cancellationToken) =>
        ListAsync("WHERE v.BathroomId = @BathroomId", new { BathroomId = bathroomId }, page, pageSize, cancellationToken);

    public Task<Paged<ReviewWithContext>> ListRecentAsync(ReviewFeedQuery query, CancellationToken cancellationToken)
    {
        var hasKeyword = !string.IsNullOrWhiteSpace(query.Keyword);

        var where = hasKeyword
            ? "WHERE (v.Body LIKE @Keyword OR v.Headline LIKE @Keyword OR b.Name LIKE @Keyword)"
            : string.Empty;

        return ListAsync(
            where,
            new { Keyword = hasKeyword ? $"%{query.Keyword!.Trim()}%" : null },
            query.Page,
            query.PageSize,
            cancellationToken);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await InsertReviewAsync(connection, review, transaction: null, cancellationToken);
    }

    public async Task ReplaceAsync(Review review, CancellationToken cancellationToken)
    {
        const string Sql = """
            UPDATE dbo.Reviews
            SET Headline = @Headline,
                Body = @Body,
                Smell = @Smell,
                Cleanliness = @Cleanliness,
                Amenities = @Amenities,
                Accessibility = @Accessibility,
                Ambience = @Ambience,
                VisitedOn = @VisitedOn
            WHERE Id = @Id
            """;

        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            Sql, ToParameters(review), cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Reviews WHERE Id = @Id",
            new { Id = reviewId },
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Shared by this repository and the bathroom repository, which inserts a review inside
    /// the same transaction when somebody drops a pin and reviews it in one go.
    /// </summary>
    internal static Task InsertReviewAsync(
        IDbConnection connection,
        Review review,
        IDbTransaction? transaction,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            InsertSql, ToParameters(review), transaction, cancellationToken: cancellationToken));

    private static object ToParameters(Review review) => new
    {
        review.Id,
        review.BathroomId,
        review.AuthorId,
        review.Headline,
        review.Body,
        Smell = (byte)review.Scores.Smell,
        Cleanliness = (byte)review.Scores.Cleanliness,
        Amenities = (byte)review.Scores.Amenities,
        Accessibility = (byte)review.Scores.Accessibility,
        Ambience = (byte)review.Scores.Ambience,
        VisitedOn = review.VisitedOn?.ToDateTime(TimeOnly.MinValue),
        review.IsSeedData,
        CreatedAtUtc = review.CreatedAt.UtcDateTime,
    };

    private async Task<Paged<ReviewWithContext>> ListAsync(
        string where, object parameters, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var sql = $"""
            SELECT COUNT(1)
            FROM dbo.Reviews v
            INNER JOIN dbo.Bathrooms b ON b.Id = v.BathroomId
            {where};

            SELECT {ReviewColumns}
            {ReviewJoins}
            {where}
            ORDER BY v.CreatedAtUtc DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var allParameters = new DynamicParameters(parameters);
        allParameters.Add("Skip", (safePage - 1) * safePageSize);
        allParameters.Add("Take", safePageSize);

        using var connection = await connections.OpenAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, allParameters, cancellationToken: cancellationToken));

        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<ReviewRow>();

        var items = rows
            .Select(r => new ReviewWithContext(r.ToDomain(), r.BathroomId, r.BathroomName, r.BathroomSlug, r.AuthorName))
            .ToList();

        return new Paged<ReviewWithContext>(items, safePage, safePageSize, total);
    }
}

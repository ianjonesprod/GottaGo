using Dapper;
using GottaGo.Application.HighScores;
using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;
using GottaGo.Infrastructure.Db;

namespace GottaGo.Infrastructure.Repositories;

/// <summary>
/// The leaderboard.
///
/// SQL computes the averages and the count; the Bayesian weighting that decides the actual
/// order is applied here by <see cref="RankingPolicy"/>, so the formula lives in one testable
/// place rather than being duplicated in a query nobody can unit test.
/// </summary>
internal sealed class HighScoreRepository(ISqlConnectionFactory connections) : IHighScoreRepository
{
    private const int MaxPageSize = 100;

    private const string Sql = """
        SELECT b.Id, b.Slug, b.Name, b.City, b.IsSeedData,
               COUNT(v.Id)                       AS ReviewCount,
               AVG(CAST(v.Smell AS FLOAT))         AS AvgSmell,
               AVG(CAST(v.Cleanliness AS FLOAT))   AS AvgCleanliness,
               AVG(CAST(v.Amenities AS FLOAT))     AS AvgAmenities,
               AVG(CAST(v.Accessibility AS FLOAT)) AS AvgAccessibility,
               AVG(CAST(v.Ambience AS FLOAT))      AS AvgAmbience
        FROM dbo.Bathrooms b
        INNER JOIN dbo.Reviews v ON v.BathroomId = b.Id
        GROUP BY b.Id, b.Slug, b.Name, b.City, b.IsSeedData
        """;

    public async Task<Paged<HighScoreEntry>> TopAsync(HighScoreQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        using var connection = await connections.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<HighScoreRow>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));

        var scored = rows
            .Select(row =>
            {
                var averages = new RatingAverages(
                    row.ReviewCount, row.AvgSmell, row.AvgCleanliness,
                    row.AvgAmenities, row.AvgAccessibility, row.AvgAmbience);

                // A single-dimension leaderboard ranks on that dimension alone; the overall
                // board uses the confidence-weighted score.
                var score = query.Dimension is { } dimension
                    ? averages[dimension]
                    : RankingPolicy.Score(averages);

                return (row, averages, score);
            })
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.row.Name)
            .ToList();

        var items = scored
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select((x, index) => new HighScoreEntry(
                Rank: ((page - 1) * pageSize) + index + 1,
                BathroomId: x.row.Id,
                Slug: x.row.Slug,
                Name: x.row.Name,
                City: x.row.City,
                Ratings: x.averages,
                RankingScore: x.score,
                IsSeedData: x.row.IsSeedData))
            .ToList();

        return new Paged<HighScoreEntry>(items, page, pageSize, scored.Count);
    }

    private sealed class HighScoreRow
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public bool IsSeedData { get; init; }
        public int ReviewCount { get; init; }
        public double AvgSmell { get; init; }
        public double AvgCleanliness { get; init; }
        public double AvgAmenities { get; init; }
        public double AvgAccessibility { get; init; }
        public double AvgAmbience { get; init; }
    }
}

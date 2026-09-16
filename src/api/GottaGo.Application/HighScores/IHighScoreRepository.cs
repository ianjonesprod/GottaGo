using GottaGo.Domain.Common;
using GottaGo.Domain.Reviews;

namespace GottaGo.Application.HighScores;

/// <summary>One row of the leaderboard.</summary>
public sealed record HighScoreEntry(
    int Rank,
    Guid BathroomId,
    string Slug,
    string Name,
    string City,
    RatingAverages Ratings,
    double RankingScore,
    bool IsSeedData);

public sealed record HighScoreQuery(RatingDimension? Dimension = null, int Page = 1, int PageSize = 25);

public interface IHighScoreRepository
{
    /// <summary>
    /// The leaderboard. Ordering happens in SQL rather than here, but the shape returned is a
    /// plain record so the application never sees how that was done.
    /// </summary>
    Task<Paged<HighScoreEntry>> TopAsync(HighScoreQuery query, CancellationToken cancellationToken);
}

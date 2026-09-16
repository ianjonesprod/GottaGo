using GottaGo.Domain.Common;

namespace GottaGo.Application.HighScores;

public sealed class HighScoreService(IHighScoreRepository highScores)
{
    public Task<Paged<HighScoreEntry>> TopAsync(HighScoreQuery query, CancellationToken cancellationToken) =>
        highScores.TopAsync(query, cancellationToken);
}

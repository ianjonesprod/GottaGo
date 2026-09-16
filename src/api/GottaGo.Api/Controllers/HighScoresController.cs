using GottaGo.Api.Common;
using GottaGo.Application.HighScores;
using GottaGo.Domain.Reviews;
using Microsoft.AspNetCore.Mvc;

namespace GottaGo.Api.Controllers;

[ApiController]
[Route("api/high-scores")]
public sealed class HighScoresController(HighScoreService highScores) : ControllerBase
{
    /// <summary>
    /// The leaderboard. Without a dimension it ranks on the confidence-weighted overall
    /// score; with one it ranks on that dimension's raw average.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<HighScoreEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<HighScoreEntryDto>>> Top(
        [FromQuery] string? dimension,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var parsed = Enum.TryParse<RatingDimension>(dimension, ignoreCase: true, out var value)
            ? value
            : (RatingDimension?)null;

        var results = await highScores.TopAsync(new HighScoreQuery(parsed, page, pageSize), cancellationToken);

        return Ok(results.ToResponse(e => e.ToDto()));
    }
}

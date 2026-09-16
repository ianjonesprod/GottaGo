using GottaGo.Api.Common;
using GottaGo.Application.Reviews;
using Microsoft.AspNetCore.Mvc;

namespace GottaGo.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(ReviewService reviews) : ControllerBase
{
    /// <summary>The list view: every review, newest first, optionally filtered by keyword.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ReviewDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ReviewDto>>> Recent(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var results = await reviews.ListRecentAsync(new ReviewFeedQuery(q, page, pageSize), cancellationToken);

        return Ok(results.ToResponse(r => r.ToDto()));
    }
}

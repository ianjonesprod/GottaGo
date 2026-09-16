using GottaGo.Api.Common;
using GottaGo.Application.Abstractions;
using GottaGo.Application.Bathrooms;
using GottaGo.Application.Reviews;
using GottaGo.Domain.Bathrooms;
using Microsoft.AspNetCore.Mvc;

namespace GottaGo.Api.Controllers;

[ApiController]
[Route("api/bathrooms")]
public sealed class BathroomsController(
    BathroomService bathrooms,
    ReviewService reviews,
    IPhotoStorage photos) : ControllerBase
{
    /// <summary>Search bathrooms by keyword, map viewport, or distance from a point.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<BathroomSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BathroomSummaryDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] double? north,
        [FromQuery] double? south,
        [FromQuery] double? east,
        [FromQuery] double? west,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radiusMiles,
        [FromQuery] string? venue,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new BathroomSearchQuery
        {
            Keyword = q,
            Bounds = north is { } n && south is { } s && east is { } e && west is { } w
                ? new MapBounds(n, s, e, w)
                : null,
            Near = lat is { } latitude && lng is { } longitude ? new GeoPoint(latitude, longitude) : null,
            RadiusMiles = radiusMiles,
            Venue = Enum.TryParse<VenueKind>(venue, ignoreCase: true, out var parsedVenue) ? parsedVenue : null,
            Sort = Enum.TryParse<BathroomSort>(sort, ignoreCase: true, out var parsedSort)
                ? parsedSort
                : BathroomSort.Rating,
            Page = page,
            PageSize = pageSize,
        };

        var results = await bathrooms.SearchAsync(query, cancellationToken);

        return Ok(results.ToResponse(b => b.ToSummaryDto(photos.GetUrl)));
    }

    /// <summary>One bathroom by id or slug, with its photo gallery.</summary>
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType<BathroomDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BathroomDetailDto>> Get(string idOrSlug, CancellationToken cancellationToken)
    {
        var bathroom = await bathrooms.GetAsync(idOrSlug, cancellationToken);

        return Ok(bathroom.ToDetailDto(photos.GetUrl));
    }

    /// <summary>Reviews for one bathroom, newest first.</summary>
    [HttpGet("{id:guid}/reviews")]
    [ProducesResponseType<PagedResponse<ReviewDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ReviewDto>>> Reviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var results = await reviews.ListForBathroomAsync(id, page, pageSize, cancellationToken);

        return Ok(results.ToResponse(r => r.ToDto()));
    }
}

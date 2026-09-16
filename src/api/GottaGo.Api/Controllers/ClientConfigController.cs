using Microsoft.AspNetCore.Mvc;

namespace GottaGo.Api.Controllers;

public sealed record ClientConfigDto(
    string? GoogleMapsApiKey,
    string? GoogleMapsMapId,
    double DefaultLatitude,
    double DefaultLongitude,
    double DefaultRadiusMiles,
    bool GoogleSignInEnabled);

/// <summary>
/// Settings the browser needs at runtime.
///
/// The Maps key is served from here rather than compiled into the bundle. It is not a secret -
/// anyone using the site can read it out of the network tab - but it is abusable and billable,
/// so keeping it out of the repository means it never enters git history and rotating it needs
/// no rebuild. Restrict it by HTTP referrer in Google Cloud; that, not secrecy, is what
/// actually protects it.
/// </summary>
[ApiController]
[Route("api/client-config")]
public sealed class ClientConfigController(IConfiguration configuration) : ControllerBase
{
    /// <summary>Downtown Cleveland, used until the visitor offers their own location.</summary>
    private const double ClevelandLatitude = 41.4993;
    private const double ClevelandLongitude = -81.6944;

    [HttpGet]
    [ProducesResponseType<ClientConfigDto>(StatusCodes.Status200OK)]
    public ActionResult<ClientConfigDto> Get() => Ok(new ClientConfigDto(
        GoogleMapsApiKey: configuration["GoogleMaps:ApiKey"],
        GoogleMapsMapId: configuration["GoogleMaps:MapId"],
        DefaultLatitude: ClevelandLatitude,
        DefaultLongitude: ClevelandLongitude,
        DefaultRadiusMiles: 20,
        GoogleSignInEnabled: !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])));
}

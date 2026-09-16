using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GottaGo.Application.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GottaGo.Api.Controllers;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(10)] string Password,
    [Required, MaxLength(100)] string DisplayName);

public sealed record SignInRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record CurrentUserDto(Guid Id, string Email, string DisplayName);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDto User);

[ApiController]
[Route("api/auth")]
// Only these endpoints are rate limited. Applying it to every controller would throttle
// ordinary browsing, which is not what it is for.
[EnableRateLimiting("auth")]
public sealed class AuthController(IIdentityService identity) : ControllerBase
{
    /// <summary>
    /// Where the refresh cookie is sent back. Scoping it to the auth routes means it is not
    /// attached to every ordinary API call, so it is exposed in far fewer places.
    /// </summary>
    private const string RefreshCookieName = "gg_refresh";
    private const string RefreshCookiePath = "/api/auth";

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var outcome = await identity.RegisterAsync(
            request.Email, request.Password, request.DisplayName, cancellationToken);

        return outcome.Succeeded
            ? Created(string.Empty, Respond(outcome.Result!))
            : Problem(outcome.Failure);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var outcome = await identity.SignInAsync(request.Email, request.Password, cancellationToken);

        return outcome.Succeeded ? Ok(Respond(outcome.Result!)) : Problem(outcome.Failure);
    }

    /// <summary>
    /// Swaps the refresh cookie for a new access token. Takes no body: the credential is the
    /// cookie, which JavaScript cannot read.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshCookieName];

        if (string.IsNullOrEmpty(token))
        {
            // Anonymous visitors hit this on every page load. It is an expected answer, not
            // an error worth logging loudly.
            return Unauthorized();
        }

        var outcome = await identity.RefreshAsync(token, cancellationToken);

        if (!outcome.Succeeded)
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });

            return Unauthorized();
        }

        return Ok(Respond(outcome.Result!));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshCookieName];

        if (!string.IsNullOrEmpty(token))
        {
            await identity.SignOutAsync(token, cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserDto> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Ok(new CurrentUserDto(
            Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.Identity?.Name ?? string.Empty));
    }

    /// <summary>
    /// Starts the Google sign-in redirect. The browser leaves the app, comes back at the
    /// callback below, and the client secret never goes near the frontend.
    /// </summary>
    [HttpGet("google/start")]
    public IActionResult StartGoogle([FromQuery] string returnUrl = "/map")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl }),
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var authentication = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authentication.Succeeded || authentication.Principal is null)
        {
            return Redirect("/sign-in?error=google");
        }

        var subject = authentication.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authentication.Principal.FindFirstValue(ClaimTypes.Email);

        if (subject is null || email is null)
        {
            return Redirect("/sign-in?error=google");
        }

        var outcome = await identity.SignInWithExternalAsync(
            GoogleDefaults.AuthenticationScheme,
            subject,
            email,
            authentication.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
            cancellationToken);

        if (!outcome.Succeeded)
        {
            return Redirect("/sign-in?error=google");
        }

        SetRefreshCookie(outcome.Result!);

        // The access token is not put in the URL: it would end up in browser history and in
        // any server log along the way. The app calls refresh once it lands.
        return Redirect($"/auth/callback?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private AuthResponse Respond(AuthResult result)
    {
        SetRefreshCookie(result);

        return new AuthResponse(
            result.AccessToken,
            result.AccessTokenExpiresAt,
            new CurrentUserDto(result.User.Id, result.User.Email, result.User.DisplayName));
    }

    private void SetRefreshCookie(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, new CookieOptions
        {
            // Unreadable to JavaScript, so an injected script cannot steal a long-lived
            // credential even if it manages to run.
            HttpOnly = true,
            // Lax rather than Strict: the Google sign-in redirect is a cross-site navigation
            // back into the app, and Strict would drop the cookie on arrival.
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Path = RefreshCookiePath,
            Expires = result.RefreshTokenExpiresAt,
        });
    }

    private IActionResult Problem(AuthFailure failure) => failure switch
    {
        AuthFailure.EmailAlreadyRegistered => BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Email already registered",
            Detail = "That email address already has an account. Try signing in instead.",
        }),
        AuthFailure.WeakPassword => BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Password too short",
            Detail = $"Use at least {IdentityService.MinimumPasswordLength} characters.",
        }),
        AuthFailure.AccountLocked => StatusCode(StatusCodes.Status423Locked, new ProblemDetails
        {
            Status = StatusCodes.Status423Locked,
            Title = "Account temporarily locked",
            Detail = "Too many failed attempts. Try again in a few minutes.",
        }),
        // Deliberately identical for an unknown email and a wrong password.
        _ => Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Sign-in failed",
            Detail = "That email and password combination didn't work.",
        }),
    };
}

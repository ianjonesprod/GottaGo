using System.Security.Claims;
using System.Text;
using GottaGo.Application.Abstractions;
using GottaGo.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GottaGo.Api.Common;

internal static class AuthenticationSetup
{
    internal static IServiceCollection AddGottaGoAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException(
                "No JWT signing key configured. Set Jwt:SigningKey with `dotnet user-secrets`. "
                + "Refusing to start with a default key, because a predictable one lets anybody mint tokens.");
        }

        var builder = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    // No grace period. The default five minutes means an expired token keeps
                    // working, which quietly undoes the point of a short lifetime.
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                };
            });

        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];

        // Google sign-in is optional. Without credentials the rest of the app still runs and
        // the sign-in page simply does not offer the button.
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            builder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
            });
        }

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}

/// <summary>Reads the signed-in user's id off the validated token.</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? accessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}

using System.Security.Claims;
using System.Text;
using GottaGo.Application.Abstractions;
using GottaGo.Application.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GottaGo.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "gottago";
    public string Audience { get; init; } = "gottago";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
}

/// <summary>
/// Issues the access token.
///
/// Fifteen minutes is deliberately short. The token is held only in memory on the client, so
/// a page refresh throws it away anyway, and a short life bounds what a leaked one is worth.
/// </summary>
internal sealed class JwtTokenService(JwtOptions options, IClock clock) : ITokenService
{
    private readonly SigningCredentials credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public (string Token, DateTimeOffset ExpiresAt) IssueAccessToken(AuthenticatedUser user)
    {
        var expiresAt = clock.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [ClaimTypes.Name] = user.DisplayName,
            },
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}

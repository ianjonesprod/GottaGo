using System.Security.Cryptography;
using System.Text;
using Dapper;
using GottaGo.Application.Abstractions;
using GottaGo.Application.Identity;
using GottaGo.Infrastructure.Db;

namespace GottaGo.Infrastructure.Identity;

/// <summary>
/// Refresh tokens.
///
/// The raw token exists in exactly two places: the response we send once, and the user's
/// cookie. What the database holds is a SHA-256 hash, so a leaked database backup does not
/// hand somebody a working set of sessions.
///
/// SHA-256 rather than a password hash is the right choice here: these are 256 bits of
/// output from a cryptographic random generator, so there is nothing to brute force and the
/// deliberate slowness of PBKDF2 would only make every refresh slower.
/// </summary>
internal sealed class RefreshTokenStore(ISqlConnectionFactory connections, IClock clock) : IRefreshTokenStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public async Task<string> IssueAsync(Guid userId, Guid? familyId, CancellationToken cancellationToken)
    {
        var raw = GenerateToken();

        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.RefreshTokens (Id, UserId, TokenHash, FamilyId, ExpiresAtUtc, CreatedAtUtc)
            VALUES (@Id, @UserId, @TokenHash, @FamilyId, @ExpiresAtUtc, @CreatedAtUtc)
            """,
            new
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = Hash(raw),
                // Rotated tokens inherit the family, so a replay can revoke the whole chain.
                FamilyId = familyId ?? Guid.NewGuid(),
                ExpiresAtUtc = clock.UtcNow.Add(Lifetime).UtcDateTime,
                CreatedAtUtc = clock.UtcNow.UtcDateTime,
            },
            cancellationToken: cancellationToken));

        return raw;
    }

    public async Task<StoredRefreshToken?> FindAsync(string rawToken, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TokenRow>(new CommandDefinition(
            """
            SELECT Id, UserId, FamilyId, ExpiresAtUtc, RevokedAtUtc
            FROM dbo.RefreshTokens
            WHERE TokenHash = @TokenHash
            """,
            new { TokenHash = Hash(rawToken) },
            cancellationToken: cancellationToken));

        return row is null
            ? null
            : new StoredRefreshToken(
                row.Id,
                row.UserId,
                row.FamilyId,
                new DateTimeOffset(row.ExpiresAtUtc, TimeSpan.Zero),
                row.RevokedAtUtc is not null);
    }

    public async Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.RefreshTokens SET RevokedAtUtc = @Now WHERE Id = @Id AND RevokedAtUtc IS NULL",
            new { Id = tokenId, Now = clock.UtcNow.UtcDateTime },
            cancellationToken: cancellationToken));
    }

    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.RefreshTokens SET RevokedAtUtc = @Now WHERE FamilyId = @FamilyId AND RevokedAtUtc IS NULL",
            new { FamilyId = familyId, Now = clock.UtcNow.UtcDateTime },
            cancellationToken: cancellationToken));
    }

    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private sealed class TokenRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid FamilyId { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
        public DateTime? RevokedAtUtc { get; init; }
    }
}

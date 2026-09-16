namespace GottaGo.Application.Identity;

/// <summary>A user as the identity code needs to see them, including the secrets it checks.</summary>
public sealed record UserCredentials(
    Guid Id,
    string Email,
    string DisplayName,
    string? PasswordHash,
    int AccessFailedCount,
    DateTimeOffset? LockoutEndUtc);

public interface IUserStore
{
    Task<UserCredentials?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<UserCredentials?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken);

    Task<UserCredentials> CreateAsync(string email, string displayName, string? passwordHash, CancellationToken cancellationToken);

    Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey, CancellationToken cancellationToken);

    /// <summary>Records a failed sign-in and locks the account once too many pile up.</summary>
    Task RecordFailedAttemptAsync(Guid userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken cancellationToken);

    Task ClearFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>Turns a password into something safe to store, and checks it later.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

/// <summary>Issues the short-lived access token.</summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) IssueAccessToken(AuthenticatedUser user);
}

public sealed record StoredRefreshToken(Guid Id, Guid UserId, Guid FamilyId, DateTimeOffset ExpiresAt, bool IsRevoked);

/// <summary>
/// Refresh tokens, stored hashed and rotated on every use.
///
/// Tokens rotated from one another share a family id. If an already-rotated token is
/// presented again it means somebody is replaying a stolen one, so the whole family is
/// revoked rather than just that token. Rotation without that check is theatre.
/// </summary>
public interface IRefreshTokenStore
{
    Task<string> IssueAsync(Guid userId, Guid? familyId, CancellationToken cancellationToken);

    Task<StoredRefreshToken?> FindAsync(string rawToken, CancellationToken cancellationToken);

    Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken);

    Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken);
}

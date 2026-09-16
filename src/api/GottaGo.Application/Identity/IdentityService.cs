using GottaGo.Application.Abstractions;

namespace GottaGo.Application.Identity;

/// <summary>
/// The identity flow itself: register, sign in, refresh, sign out.
///
/// Every cryptographic operation is delegated - password hashing, token signing, random
/// generation - so what lives here is only the policy. That is the part worth owning and
/// reading; the primitives are not.
/// </summary>
public sealed class IdentityService(
    IUserStore users,
    IPasswordHasher passwords,
    ITokenService tokens,
    IRefreshTokenStore refreshTokens,
    IClock clock) : IIdentityService
{
    /// <summary>Minimum password length. Length beats character-class rules, and it is easier to explain.</summary>
    public const int MinimumPasswordLength = 10;

    private const int LockoutThreshold = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthOutcome> RegisterAsync(
        string email, string password, string displayName, CancellationToken cancellationToken)
    {
        if (password.Length < MinimumPasswordLength)
        {
            return AuthOutcome.Failed(AuthFailure.WeakPassword);
        }

        var normalised = Normalise(email);

        if (await users.FindByEmailAsync(normalised, cancellationToken) is not null)
        {
            return AuthOutcome.Failed(AuthFailure.EmailAlreadyRegistered);
        }

        var created = await users.CreateAsync(normalised, displayName, passwords.Hash(password), cancellationToken);

        return AuthOutcome.Success(await IssueAsync(created, familyId: null, cancellationToken));
    }

    public async Task<AuthOutcome> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(Normalise(email), cancellationToken);

        // Same answer whether the account does not exist or the password is wrong, so this
        // endpoint cannot be used to find out which email addresses are registered.
        if (user?.PasswordHash is null)
        {
            return AuthOutcome.Failed(AuthFailure.InvalidCredentials);
        }

        if (user.LockoutEndUtc is { } lockoutEnd && lockoutEnd > clock.UtcNow)
        {
            return AuthOutcome.Failed(AuthFailure.AccountLocked);
        }

        if (!passwords.Verify(password, user.PasswordHash))
        {
            await users.RecordFailedAttemptAsync(user.Id, LockoutThreshold, LockoutDuration, cancellationToken);

            return AuthOutcome.Failed(AuthFailure.InvalidCredentials);
        }

        await users.ClearFailedAttemptsAsync(user.Id, cancellationToken);

        return AuthOutcome.Success(await IssueAsync(user, familyId: null, cancellationToken));
    }

    public async Task<AuthOutcome> SignInWithExternalAsync(
        string provider, string providerKey, string email, string displayName, CancellationToken cancellationToken)
    {
        var existing = await users.FindByExternalLoginAsync(provider, providerKey, cancellationToken);

        if (existing is not null)
        {
            return AuthOutcome.Success(await IssueAsync(existing, familyId: null, cancellationToken));
        }

        var normalised = Normalise(email);

        // Somebody who registered with a password and later signs in with Google gets the
        // provider linked to their existing account rather than a confusing duplicate.
        var byEmail = await users.FindByEmailAsync(normalised, cancellationToken);

        if (byEmail is not null)
        {
            await users.LinkExternalLoginAsync(byEmail.Id, provider, providerKey, cancellationToken);

            return AuthOutcome.Success(await IssueAsync(byEmail, familyId: null, cancellationToken));
        }

        // No password: this account can only ever be reached through the provider.
        var created = await users.CreateAsync(normalised, displayName, passwordHash: null, cancellationToken);

        await users.LinkExternalLoginAsync(created.Id, provider, providerKey, cancellationToken);
        return AuthOutcome.Success(await IssueAsync(created, familyId: null, cancellationToken));
    }

    public async Task<AuthOutcome> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.FindAsync(refreshToken, cancellationToken);

        if (stored is null || stored.ExpiresAt <= clock.UtcNow)
        {
            return AuthOutcome.Failed(AuthFailure.InvalidCredentials);
        }

        // An already-revoked token being presented means one was replayed, which means one
        // leaked. Kill the whole chain rather than just this token.
        if (stored.IsRevoked)
        {
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, cancellationToken);

            return AuthOutcome.Failed(AuthFailure.InvalidCredentials);
        }

        var user = await users.FindByIdAsync(stored.UserId, cancellationToken);

        if (user is null)
        {
            return AuthOutcome.Failed(AuthFailure.InvalidCredentials);
        }

        await refreshTokens.RevokeAsync(stored.Id, cancellationToken);

        return AuthOutcome.Success(await IssueAsync(user, stored.FamilyId, cancellationToken));
    }

    public async Task SignOutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.FindAsync(refreshToken, cancellationToken);

        if (stored is not null)
        {
            // Sign out everywhere that chain reaches, not just this tab.
            await refreshTokens.RevokeFamilyAsync(stored.FamilyId, cancellationToken);
        }
    }

    private async Task<AuthResult> IssueAsync(UserCredentials user, Guid? familyId, CancellationToken cancellationToken)
    {
        var authenticated = new AuthenticatedUser(user.Id, user.Email, user.DisplayName);
        var (accessToken, expiresAt) = tokens.IssueAccessToken(authenticated);
        var refreshToken = await refreshTokens.IssueAsync(user.Id, familyId, cancellationToken);

        return new AuthResult(
            authenticated,
            accessToken,
            expiresAt,
            refreshToken,
            clock.UtcNow.AddDays(7));
    }

    /// <summary>Emails are case-insensitive in practice, so store and compare one way.</summary>
    private static string Normalise(string email) => email.Trim().ToLowerInvariant();
}

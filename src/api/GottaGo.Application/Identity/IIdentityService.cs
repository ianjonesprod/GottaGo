namespace GottaGo.Application.Identity;

public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName);

/// <summary>What the caller gets back: a short-lived token plus the raw refresh token.</summary>
public sealed record AuthResult(
    AuthenticatedUser User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>Sign-up and sign-in failures, kept deliberately coarse. See the note below.</summary>
public enum AuthFailure
{
    None = 0,
    InvalidCredentials,
    EmailAlreadyRegistered,
    AccountLocked,
    WeakPassword,
}

public sealed record AuthOutcome(AuthResult? Result, AuthFailure Failure)
{
    public bool Succeeded => Result is not null;

    public static AuthOutcome Success(AuthResult result) => new(result, AuthFailure.None);

    public static AuthOutcome Failed(AuthFailure failure) => new(null, failure);
}

/// <summary>
/// Registration, sign-in and linking an external login.
///
/// Note that sign-in returns <see cref="AuthFailure.InvalidCredentials"/> for both an unknown
/// email and a wrong password. Distinguishing them would turn this endpoint into a way to
/// discover which email addresses have accounts, which is the most common mistake in a
/// hand-written sign-in.
/// </summary>
public interface IIdentityService
{
    Task<AuthOutcome> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken);

    Task<AuthOutcome> SignInAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Finds the user behind an external login, creating one on first sign-in.</summary>
    Task<AuthOutcome> SignInWithExternalAsync(
        string provider, string providerKey, string email, string displayName, CancellationToken cancellationToken);

    /// <summary>Exchanges a refresh token for a new pair, invalidating the old one.</summary>
    Task<AuthOutcome> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task SignOutAsync(string refreshToken, CancellationToken cancellationToken);
}

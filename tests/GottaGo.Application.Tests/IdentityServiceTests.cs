using GottaGo.Application.Abstractions;
using GottaGo.Application.Identity;
using Shouldly;

namespace GottaGo.Application.Tests;

/*
    The two places a hand-written sign-in usually goes wrong: telling an attacker which email
    addresses exist, and letting them guess passwords forever. Both are pinned here.

    Hand-written fakes rather than a mocking framework, and no database - which is only
    possible because the identity service depends on interfaces it owns.
*/



public class IdentityServiceTests
{
    private const string GoodPassword = "correcthorsebattery";

    [Fact]
    public async Task An_unknown_email_and_a_wrong_password_fail_identically()
    {
        var service = Build(out var users);
        await service.RegisterAsync("real@example.com", GoodPassword, "Real", default);

        var wrongPassword = await service.SignInAsync("real@example.com", "notthepassword", default);
        var unknownEmail = await service.SignInAsync("ghost@example.com", "notthepassword", default);

        // If these differed, the endpoint would tell an attacker which addresses are registered.
        wrongPassword.Failure.ShouldBe(unknownEmail.Failure);
        wrongPassword.Failure.ShouldBe(AuthFailure.InvalidCredentials);
    }

    [Fact]
    public async Task Repeated_failures_lock_the_account()
    {
        var service = Build(out var users);
        await service.RegisterAsync("lock@example.com", GoodPassword, "Lock", default);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.SignInAsync("lock@example.com", "wrong", default);
        }

        var afterLockout = await service.SignInAsync("lock@example.com", GoodPassword, default);

        // Even the correct password is refused while locked, which is the whole point.
        afterLockout.Failure.ShouldBe(AuthFailure.AccountLocked);
    }

    [Fact]
    public async Task A_successful_sign_in_clears_the_failure_count()
    {
        var service = Build(out var users);
        await service.RegisterAsync("reset@example.com", GoodPassword, "Reset", default);

        await service.SignInAsync("reset@example.com", "wrong", default);
        await service.SignInAsync("reset@example.com", "wrong", default);
        await service.SignInAsync("reset@example.com", GoodPassword, default);

        users.Find("reset@example.com").AccessFailedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Email_case_does_not_matter()
    {
        var service = Build(out _);
        await service.RegisterAsync("Mixed@Example.COM", GoodPassword, "Mixed", default);

        var outcome = await service.SignInAsync("mIxEd@eXaMpLe.com", GoodPassword, default);

        outcome.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task The_same_email_cannot_register_twice()
    {
        var service = Build(out _);
        await service.RegisterAsync("dupe@example.com", GoodPassword, "First", default);

        var second = await service.RegisterAsync("DUPE@example.com", GoodPassword, "Second", default);

        second.Failure.ShouldBe(AuthFailure.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task A_short_password_is_rejected()
    {
        var service = Build(out _);

        var outcome = await service.RegisterAsync("short@example.com", "tooshort", "Short", default);

        outcome.Failure.ShouldBe(AuthFailure.WeakPassword);
    }

    [Fact]
    public async Task Signing_in_with_Google_links_to_an_existing_account_rather_than_duplicating_it()
    {
        var service = Build(out var users);
        await service.RegisterAsync("both@example.com", GoodPassword, "Both", default);
        var originalId = users.Find("both@example.com").Id;

        var outcome = await service.SignInWithExternalAsync("Google", "google-123", "both@example.com", "Both", default);

        outcome.Succeeded.ShouldBeTrue();
        outcome.Result!.User.Id.ShouldBe(originalId);
    }

    [Fact]
    public async Task Replaying_a_used_refresh_token_kills_the_whole_chain()
    {
        var service = Build(out _, out var refreshTokens);
        var registered = await service.RegisterAsync("chain@example.com", GoodPassword, "Chain", default);
        var first = registered.Result!.RefreshToken;

        var rotated = await service.RefreshAsync(first, default);
        var second = rotated.Result!.RefreshToken;

        // Somebody replays the token that was already exchanged - a sign one leaked.
        var replay = await service.RefreshAsync(first, default);
        replay.Succeeded.ShouldBeFalse();

        // The legitimate holder's current token is revoked too. Rotation without this is
        // theatre: the thief would simply keep using the newer token.
        var afterReplay = await service.RefreshAsync(second, default);
        afterReplay.Succeeded.ShouldBeFalse();
    }

    private static IdentityService Build(out FakeUserStore users) => Build(out users, out _);

    private static IdentityService Build(out FakeUserStore users, out FakeRefreshTokenStore refreshTokens)
    {
        var clock = new FixedClock();

        users = new FakeUserStore(clock);
        refreshTokens = new FakeRefreshTokenStore(clock);

        return new IdentityService(users, new FakePasswordHasher(), new FakeTokenService(), refreshTokens, clock);
    }
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
}

/// <summary>Reversible on purpose: these tests are about policy, not about hashing.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}

internal sealed class FakeTokenService : ITokenService
{
    public (string Token, DateTimeOffset ExpiresAt) IssueAccessToken(AuthenticatedUser user) =>
        ($"token-for-{user.Id}", DateTimeOffset.UtcNow.AddMinutes(15));
}

internal sealed class FakeUserStore(FixedClock clock) : IUserStore
{
    private readonly Dictionary<Guid, UserCredentials> users = [];
    private readonly Dictionary<(string Provider, string Key), Guid> externalLogins = [];

    public UserCredentials Find(string email) =>
        users.Values.Single(u => u.Email == email.ToLowerInvariant());

    public Task<UserCredentials?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(users.GetValueOrDefault(id));

    public Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(users.Values.FirstOrDefault(u => u.Email == email));

    public Task<UserCredentials?> FindByExternalLoginAsync(
        string provider, string providerKey, CancellationToken cancellationToken) =>
        Task.FromResult(externalLogins.TryGetValue((provider, providerKey), out var id)
            ? users.GetValueOrDefault(id)
            : null);

    public Task<UserCredentials> CreateAsync(
        string email, string displayName, string? passwordHash, CancellationToken cancellationToken)
    {
        var user = new UserCredentials(Guid.NewGuid(), email, displayName, passwordHash, 0, null);
        users[user.Id] = user;

        return Task.FromResult(user);
    }

    public Task LinkExternalLoginAsync(
        Guid userId, string provider, string providerKey, CancellationToken cancellationToken)
    {
        externalLogins[(provider, providerKey)] = userId;

        return Task.CompletedTask;
    }

    public Task RecordFailedAttemptAsync(
        Guid userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        var user = users[userId];
        var failures = user.AccessFailedCount + 1;

        users[userId] = user with
        {
            AccessFailedCount = failures,
            LockoutEndUtc = failures >= lockoutThreshold ? clock.UtcNow.Add(lockoutDuration) : user.LockoutEndUtc,
        };

        return Task.CompletedTask;
    }

    public Task ClearFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        users[userId] = users[userId] with { AccessFailedCount = 0, LockoutEndUtc = null };

        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenStore(FixedClock clock) : IRefreshTokenStore
{
    private readonly Dictionary<string, StoredRefreshToken> tokens = [];
    private int counter;

    public Task<string> IssueAsync(Guid userId, Guid? familyId, CancellationToken cancellationToken)
    {
        var raw = $"refresh-{++counter}";

        tokens[raw] = new StoredRefreshToken(
            Guid.NewGuid(), userId, familyId ?? Guid.NewGuid(), clock.UtcNow.AddDays(7), IsRevoked: false);

        return Task.FromResult(raw);
    }

    public Task<StoredRefreshToken?> FindAsync(string rawToken, CancellationToken cancellationToken) =>
        Task.FromResult(tokens.GetValueOrDefault(rawToken));

    public Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        foreach (var (raw, token) in tokens.Where(t => t.Value.Id == tokenId).ToList())
        {
            tokens[raw] = token with { IsRevoked = true };
        }

        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        foreach (var (raw, token) in tokens.Where(t => t.Value.FamilyId == familyId).ToList())
        {
            tokens[raw] = token with { IsRevoked = true };
        }

        return Task.CompletedTask;
    }
}

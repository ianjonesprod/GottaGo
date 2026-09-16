using Dapper;
using GottaGo.Application.Abstractions;
using GottaGo.Application.Identity;
using GottaGo.Infrastructure.Db;

namespace GottaGo.Infrastructure.Identity;

internal sealed class UserStore(ISqlConnectionFactory connections, IClock clock) : IUserStore
{
    private const string Columns = "Id, Email, DisplayName, PasswordHash, AccessFailedCount, LockoutEndUtc";

    public Task<UserCredentials?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        QuerySingleAsync($"SELECT {Columns} FROM dbo.Users WHERE Id = @Value", id, cancellationToken);

    public Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        QuerySingleAsync($"SELECT {Columns} FROM dbo.Users WHERE Email = @Value", email, cancellationToken);

    public async Task<UserCredentials?> FindByExternalLoginAsync(
        string provider, string providerKey, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            $"""
            SELECT u.Id, u.Email, u.DisplayName, u.PasswordHash, u.AccessFailedCount, u.LockoutEndUtc
            FROM dbo.Users u
            INNER JOIN dbo.ExternalLogins e ON e.UserId = u.Id
            WHERE e.Provider = @Provider AND e.ProviderKey = @ProviderKey
            """,
            new { Provider = provider, ProviderKey = providerKey },
            cancellationToken: cancellationToken));

        return row?.ToCredentials();
    }

    public async Task<UserCredentials> CreateAsync(
        string email, string displayName, string? passwordHash, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.Users (Id, Email, DisplayName, PasswordHash, EmailConfirmed, CreatedAtUtc)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, 1, @CreatedAtUtc)
            """,
            new
            {
                Id = id,
                Email = email,
                DisplayName = displayName,
                PasswordHash = passwordHash,
                CreatedAtUtc = clock.UtcNow.UtcDateTime,
            },
            cancellationToken: cancellationToken));

        return new UserCredentials(id, email, displayName, passwordHash, 0, null);
    }

    public async Task LinkExternalLoginAsync(
        Guid userId, string provider, string providerKey, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.ExternalLogins (Provider, ProviderKey, UserId, CreatedAtUtc)
            VALUES (@Provider, @ProviderKey, @UserId, @CreatedAtUtc)
            """,
            new
            {
                Provider = provider,
                ProviderKey = providerKey,
                UserId = userId,
                CreatedAtUtc = clock.UtcNow.UtcDateTime,
            },
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Counts a failed sign-in and locks the account once the threshold is reached. Done in
    /// one statement so two simultaneous attempts cannot both read the old count and lose one.
    /// </summary>
    public async Task RecordFailedAttemptAsync(
        Guid userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE dbo.Users
            SET AccessFailedCount = AccessFailedCount + 1,
                LockoutEndUtc = CASE
                    WHEN AccessFailedCount + 1 >= @Threshold THEN @LockoutEnd
                    ELSE LockoutEndUtc
                END
            WHERE Id = @Id
            """,
            new
            {
                Id = userId,
                Threshold = lockoutThreshold,
                LockoutEnd = clock.UtcNow.Add(lockoutDuration).UtcDateTime,
            },
            cancellationToken: cancellationToken));
    }

    public async Task ClearFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Users SET AccessFailedCount = 0, LockoutEndUtc = NULL WHERE Id = @Id",
            new { Id = userId },
            cancellationToken: cancellationToken));
    }

    private async Task<UserCredentials?> QuerySingleAsync(string sql, object value, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Value = value }, cancellationToken: cancellationToken));

        return row?.ToCredentials();
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? PasswordHash { get; init; }
        public int AccessFailedCount { get; init; }
        public DateTime? LockoutEndUtc { get; init; }

        public UserCredentials ToCredentials() => new(
            Id,
            Email,
            DisplayName,
            PasswordHash,
            AccessFailedCount,
            LockoutEndUtc is null ? null : new DateTimeOffset(LockoutEndUtc.Value, TimeSpan.Zero));
    }
}

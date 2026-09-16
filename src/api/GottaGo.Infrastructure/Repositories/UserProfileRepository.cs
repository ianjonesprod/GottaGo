using Dapper;
using GottaGo.Application.Users;
using GottaGo.Domain.Users;
using GottaGo.Infrastructure.Db;

namespace GottaGo.Infrastructure.Repositories;

internal sealed class UserProfileRepository(ISqlConnectionFactory connections) : IUserProfileRepository
{
    private const string SelectById = """
        SELECT Id, DisplayName, IsSeedUser, CreatedAtUtc
        FROM dbo.Users
        WHERE Id = @Id
        """;

    private const string Insert = """
        INSERT INTO dbo.Users (Id, Email, DisplayName, IsSeedUser, CreatedAtUtc)
        VALUES (@Id, @Email, @DisplayName, @IsSeedUser, @CreatedAtUtc)
        """;

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(SelectById, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            Insert,
            new
            {
                profile.Id,
                Email = $"{profile.Id}@placeholder.local",
                profile.DisplayName,
                profile.IsSeedUser,
                CreatedAtUtc = profile.CreatedAt.UtcDateTime,
            },
            cancellationToken: cancellationToken));
    }
}

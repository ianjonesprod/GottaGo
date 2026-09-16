using System.Data;

namespace GottaGo.Infrastructure.Db;

/// <summary>Hands out open connections. Internal to infrastructure - nothing above sees it.</summary>
public interface ISqlConnectionFactory
{
    Task<IDbConnection> OpenAsync(CancellationToken cancellationToken);
}

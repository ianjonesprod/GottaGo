using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace GottaGo.Infrastructure.Db;

/// <summary>
/// Applies the numbered SQL scripts in Db/Scripts. DbUp records what it has already run in a
/// SchemaVersions table, so running this repeatedly is safe and only new scripts execute.
/// </summary>
public static class DatabaseMigrator
{
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}

using Microsoft.Extensions.Configuration;

namespace GottaGo.Infrastructure.Db;

public static class ConnectionStrings
{
    public const string Name = "GottaGo";

    public static string GottaGo(IConfiguration configuration) =>
        configuration.GetConnectionString(Name)
        ?? throw new InvalidOperationException(
            $"No '{Name}' connection string configured. Set ConnectionStrings:{Name} in "
            + "appsettings.Development.json or as an environment variable.");
}

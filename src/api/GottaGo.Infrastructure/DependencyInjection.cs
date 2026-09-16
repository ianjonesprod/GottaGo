using GottaGo.Application.Abstractions;
using GottaGo.Application.Bathrooms;
using GottaGo.Application.HighScores;
using GottaGo.Application.Reviews;
using GottaGo.Application.Users;
using GottaGo.Infrastructure.Db;
using GottaGo.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GottaGo.Infrastructure;

/// <summary>
/// Binds the application layer's interfaces to their Dapper implementations. This is the only
/// place the two sides are introduced to each other.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStrings.GottaGo(configuration);

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IBathroomRepository, BathroomRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IHighScoreRepository, HighScoreRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }
}

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

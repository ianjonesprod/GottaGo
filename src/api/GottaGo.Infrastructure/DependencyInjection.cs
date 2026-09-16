using GottaGo.Application.Abstractions;
using GottaGo.Application.Bathrooms;
using GottaGo.Application.HighScores;
using GottaGo.Application.Reviews;
using GottaGo.Application.Users;
using GottaGo.Application.Identity;
using GottaGo.Infrastructure.Db;
using GottaGo.Infrastructure.Identity;
using GottaGo.Infrastructure.Repositories;
using GottaGo.Infrastructure.Seed;
using GottaGo.Infrastructure.Storage;
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
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPhotoStorage, StaticAssetPhotoStorage>();

        services.AddScoped<IBathroomRepository, BathroomRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IHighScoreRepository, HighScoreRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<DemoSeeder>();

        // Identity: our own flow, but every primitive comes from a maintained library.
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

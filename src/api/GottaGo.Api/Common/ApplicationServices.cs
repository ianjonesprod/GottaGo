using GottaGo.Application.Bathrooms;
using GottaGo.Application.HighScores;
using GottaGo.Application.Reviews;

namespace GottaGo.Api.Common;

/// <summary>
/// Registers the application services.
///
/// This lives in the API rather than in the application project on purpose. Putting an
/// AddApplication() extension next to the services would mean referencing a dependency
/// injection package there, and the whole point of that layer is that it depends on nothing
/// but the domain. Wiring is a composition-root job, so it happens here.
/// </summary>
internal static class ApplicationServices
{
    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<BathroomService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<HighScoreService>();

        return services;
    }
}

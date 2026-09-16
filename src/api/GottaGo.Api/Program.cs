using GottaGo.Api.Common;
using GottaGo.Infrastructure;
using GottaGo.Infrastructure.Db;
using GottaGo.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// The only line in the solution that names an infrastructure type. Everything else talks to
// the interfaces the application layer owns.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// `dotnet run -- --migrate` applies pending SQL scripts and exits without serving.
if (args.Contains("--migrate"))
{
    var result = DatabaseMigrator.Run(ConnectionStrings.GottaGo(builder.Configuration));

    if (!result.Successful)
    {
        app.Logger.LogError(result.Error, "Database migration failed.");
        return 1;
    }

    app.Logger.LogInformation("Database is up to date.");
    return 0;
}

// `dotnet run -- --seed` loads the Cleveland demo dataset, `--seed --clear` removes it.
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();

    if (args.Contains("--clear"))
    {
        await seeder.ClearAsync(CancellationToken.None);
        app.Logger.LogInformation("Demo data removed.");

        return 0;
    }

    var count = await seeder.SeedAsync(app.Environment.IsProduction(), CancellationToken.None);
    app.Logger.LogInformation("Seeded {Count} bathrooms with demo reviews.", count);

    return 0;
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json, Swagger UI at /swagger.
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "GottaGo API v1"));
}
else
{
    // Skipped in development so `ng serve` can proxy over plain HTTP without a dev
    // certificate. Trusting a dev cert writes to the machine store, outside this repo.
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

return 0;

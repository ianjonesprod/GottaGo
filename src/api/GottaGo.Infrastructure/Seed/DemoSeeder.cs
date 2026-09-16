using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using GottaGo.Domain.Bathrooms;
using GottaGo.Domain.Common;
using GottaGo.Infrastructure.Db;
using Microsoft.Data.SqlClient;

namespace GottaGo.Infrastructure.Seed;

/// <summary>
/// Loads the demo dataset: real Cleveland venues, invented reviewers, invented opinions.
///
/// Everything it writes is flagged IsSeedData so the UI can label it. The reviews are about
/// real, named places, so they must never be mistaken for what actual visitors said.
///
/// Deliberately not part of the migration chain. Migrations carry schema, which every
/// environment needs; demo content is not.
/// </summary>
public sealed class DemoSeeder(ISqlConnectionFactory connections)
{
    private const string SeedResource = "GottaGo.Infrastructure.Seed.cleveland-bathrooms.json";

    private static readonly string[] DemoUserNames =
    [
        "Ada P.", "Marcus T.", "Priya N.", "Jonah R.", "Elena V.",
        "Theo K.", "Simone A.", "Dev S.", "Rosa L.", "Casey M.",
    ];

    private static readonly string[] Openers =
    [
        "Stopped in on a weekday afternoon.",
        "Ducked in while walking past.",
        "Needed this one badly and it delivered.",
        "Used it twice in one visit.",
        "Found it easily from the main entrance.",
        "Came here after a long walk.",
    ];

    private static readonly string[] Observations =
    [
        "Clean, well stocked, and the door actually latched.",
        "Busy but the queue moved quickly.",
        "Bright lighting and a working hand dryer.",
        "A bit tired around the edges but perfectly usable.",
        "Roomy stall and a solid grab rail.",
        "Soap dispensers were full, which is more than I can say for most.",
        "Smelled strongly of cleaning product rather than anything worse.",
        "Floor was wet in one corner, otherwise fine.",
    ];

    public async Task<int> SeedAsync(bool isProduction, CancellationToken cancellationToken)
    {
        if (isProduction)
        {
            throw new InvalidOperationException(
                "Refusing to seed demo data in production. These are invented reviews about real "
                + "businesses and they must never appear to real users as genuine opinions.");
        }

        var bathrooms = LoadSeedFile();

        using var connection = (SqlConnection)await connections.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var userIds = await SeedUsersAsync(connection, transaction, cancellationToken);
            var seeded = 0;

            foreach (var entry in bathrooms)
            {
                await SeedBathroomAsync(connection, transaction, entry, userIds, cancellationToken);
                seeded++;
            }

            transaction.Commit();

            return seeded;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>Removes everything the seeder created, leaving real user content alone.</summary>
    public async Task<int> ClearAsync(CancellationToken cancellationToken)
    {
        using var connection = await connections.OpenAsync(cancellationToken);

        return await connection.ExecuteAsync(new CommandDefinition(
            """
            DELETE FROM dbo.Reviews WHERE IsSeedData = 1;
            DELETE FROM dbo.BathroomPhotos WHERE IsSeedData = 1;
            DELETE FROM dbo.Bathrooms WHERE IsSeedData = 1;
            DELETE FROM dbo.Users WHERE IsSeedUser = 1;
            """,
            cancellationToken: cancellationToken));
    }

    private static IReadOnlyList<SeedBathroom> LoadSeedFile()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SeedResource)
            ?? throw new InvalidOperationException($"Seed file '{SeedResource}' is missing from the assembly.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<List<SeedBathroom>>(stream, options)
            ?? throw new InvalidOperationException("Seed file could not be read.");
    }

    private static async Task<IReadOnlyList<Guid>> SeedUsersAsync(
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();

        foreach (var name in DemoUserNames)
        {
            // A deterministic id means re-running the seeder updates the same rows rather
            // than piling up duplicate reviewers.
            var id = DeterministicGuid($"user:{name}");
            var email = $"seed+{Slug.From(name)}@gottago.local";

            await connection.ExecuteAsync(new CommandDefinition(
                """
                MERGE dbo.Users AS target
                USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
                WHEN MATCHED THEN UPDATE SET DisplayName = @DisplayName
                WHEN NOT MATCHED THEN
                    INSERT (Id, Email, DisplayName, EmailConfirmed, IsSeedUser, CreatedAtUtc)
                    VALUES (@Id, @Email, @DisplayName, 1, 1, @CreatedAtUtc);
                """,
                new { Id = id, Email = email, DisplayName = name, CreatedAtUtc = DateTime.UtcNow },
                transaction,
                cancellationToken: cancellationToken));

            ids.Add(id);
        }

        return ids;
    }

    private static async Task SeedBathroomAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        SeedBathroom entry,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var bathroomId = DeterministicGuid($"bathroom:{entry.Slug}");
        var venue = Enum.TryParse<VenueKind>(entry.Venue, ignoreCase: true, out var parsed) ? parsed : VenueKind.Other;

        await connection.ExecuteAsync(new CommandDefinition(
            """
            MERGE dbo.Bathrooms AS target
            USING (SELECT @Slug AS Slug) AS source ON target.Slug = source.Slug
            WHEN MATCHED THEN UPDATE SET
                Name = @Name, Description = @Description, Street = @Street, City = @City,
                State = @State, PostalCode = @PostalCode, Latitude = @Latitude,
                Longitude = @Longitude, Venue = @Venue, AccessNote = @AccessNote
            WHEN NOT MATCHED THEN
                INSERT (Id, Slug, Name, Description, Street, City, State, PostalCode,
                        Latitude, Longitude, Venue, AccessNote, IsSeedData, CreatedAtUtc)
                VALUES (@Id, @Slug, @Name, @Description, @Street, @City, @State, @PostalCode,
                        @Latitude, @Longitude, @Venue, @AccessNote, 1, @CreatedAtUtc);
            """,
            new
            {
                Id = bathroomId,
                entry.Slug,
                entry.Name,
                entry.Description,
                entry.Street,
                entry.City,
                entry.State,
                entry.PostalCode,
                entry.Latitude,
                entry.Longitude,
                Venue = (int)venue,
                entry.AccessNote,
                CreatedAtUtc = DateTime.UtcNow,
            },
            transaction,
            cancellationToken: cancellationToken));

        await SeedReviewsAsync(connection, transaction, entry, bathroomId, userIds, cancellationToken);
        await SeedPhotosAsync(connection, transaction, entry, bathroomId, cancellationToken);
    }

    private static async Task SeedReviewsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        SeedBathroom entry,
        Guid bathroomId,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        // Seeded from the slug so the same bathroom always gets the same reviews and scores.
        // Without that, every seed run would change the leaderboard and no end-to-end test
        // could assert a rating.
        var random = new Random(StableHash.Of(entry.Slug));
        var reviewCount = random.Next(3, 7);
        var reviewers = userIds.OrderBy(_ => random.Next()).Take(reviewCount).ToList();

        foreach (var authorId in reviewers)
        {
            var body = $"{Openers[random.Next(Openers.Length)]} {Observations[random.Next(Observations.Length)]}";

            await connection.ExecuteAsync(new CommandDefinition(
                """
                MERGE dbo.Reviews AS target
                USING (SELECT @BathroomId AS BathroomId, @AuthorId AS AuthorId) AS source
                    ON target.BathroomId = source.BathroomId AND target.AuthorId = source.AuthorId
                WHEN MATCHED THEN UPDATE SET
                    Body = @Body, Smell = @Smell, Cleanliness = @Cleanliness,
                    Amenities = @Amenities, Accessibility = @Accessibility, Ambience = @Ambience
                WHEN NOT MATCHED THEN
                    INSERT (Id, BathroomId, AuthorId, Body, Smell, Cleanliness, Amenities,
                            Accessibility, Ambience, IsSeedData, CreatedAtUtc)
                    VALUES (@Id, @BathroomId, @AuthorId, @Body, @Smell, @Cleanliness, @Amenities,
                            @Accessibility, @Ambience, 1, @CreatedAtUtc);
                """,
                new
                {
                    Id = DeterministicGuid($"review:{entry.Slug}:{authorId}"),
                    BathroomId = bathroomId,
                    AuthorId = authorId,
                    Body = body,
                    Smell = (byte)random.Next(2, 6),
                    Cleanliness = (byte)random.Next(2, 6),
                    Amenities = (byte)random.Next(1, 6),
                    Accessibility = (byte)random.Next(3, 6),
                    Ambience = (byte)random.Next(1, 6),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 200)),
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }

    private static async Task SeedPhotosAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        SeedBathroom entry,
        Guid bathroomId,
        CancellationToken cancellationToken)
    {
        // Placeholder artwork rather than photographs. These are real named venues, and a
        // stock photo implied to be "the restroom at X" would be a small lie; an obvious
        // placeholder is not.
        for (var index = 1; index <= 3; index++)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                MERGE dbo.BathroomPhotos AS target
                USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
                WHEN NOT MATCHED THEN
                    INSERT (Id, BathroomId, BlobName, AltText, SortOrder, IsSeedData, CreatedAtUtc)
                    VALUES (@Id, @BathroomId, @BlobName, @AltText, @SortOrder, 1, @CreatedAtUtc);
                """,
                new
                {
                    Id = DeterministicGuid($"photo:{entry.Slug}:{index}"),
                    BathroomId = bathroomId,
                    BlobName = $"placeholder-{index}.svg",
                    AltText = $"Placeholder illustration {index} of 3 for {entry.Name}. "
                        + "No photograph of this restroom is available.",
                    SortOrder = index,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }

    /// <summary>Same input, same GUID, every run - so seeding is an update rather than a duplicate.</summary>
    private static Guid DeterministicGuid(string key)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));

        return new Guid(bytes);
    }
}

using System.Collections.Concurrent;
using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources.Commons;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Grimoire.Services;

public class DatabaseBackgroundService(
    IDocumentStore documentStore,
    ILogger<DatabaseBackgroundService> logger,
    DatabaseHandler databaseHandler,
    IEnumerable<IGrimoireSource> grimoireSources,
    ServiceCoordinator serviceCoordinator,
    IConfiguration configuration) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            var result = await documentStore
                .Maintenance
                .Server
                .SendAsync(new CreateDatabaseOperation(new DatabaseRecord(nameof(Grimoire))), stoppingToken);

            logger.LogInformation("Created database {database}", result.Name);
        }
        catch {
            logger.LogWarning("Database already exists.");
        }

        try {
            await documentStore
                .Maintenance
                .SendAsync(new ConfigureRevisionsOperation(new RevisionsConfiguration {
                    Default = new RevisionsCollectionConfiguration {
                        Disabled = false
                    }
                }), stoppingToken);
        }
        catch {
            logger.LogWarning("Revisions already enabled.");
        }

        var defaultUsername = configuration.GetValue<string>("Library:DefaultUsername");
        var existingUser = await databaseHandler.GetUserAsync(defaultUsername!.GetIdFromName());
        if (existingUser is null) {
            await databaseHandler.UpsertUserAsync(new UserObject {
                Username = defaultUsername!,
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                Library = new ConcurrentDictionary<string, float>()
            });
        }

        foreach (var source in grimoireSources) {
            var existing = await databaseHandler.GetSourceAsync(source.Name.GetIdFromName());
            if (existing != default) {
                continue;
            }

            logger.LogInformation("Registering source '{}'.", source.Name);
            await databaseHandler.StoreAsync(new SourceObject(
                source.Name,
                source.Url,
                source.Icon,
                DateTime.UtcNow,
                false));
        }

        serviceCoordinator.ServiceIsReady();
    }
}
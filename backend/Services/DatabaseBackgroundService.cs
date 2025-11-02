using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Grimoire.Services;

public class DatabaseBackgroundService(
    IDocumentStore documentStore,
    ILogger<DatabaseBackgroundService> logger,
    ServiceCoodrinator serviceCoodrinator) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            var result = await documentStore
                .Maintenance
                .Server
                .SendAsync(new CreateDatabaseOperation(new DatabaseRecord(nameof(Grimoire))), stoppingToken);
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

        serviceCoodrinator.ServiceIsReady();
    }
}
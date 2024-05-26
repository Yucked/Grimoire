using Grimoire.Objects;
using Grimoire.Sources;
using LiteDB;

namespace Grimoire.Handlers;

public sealed class BackgroundHandler(
    ILiteDatabase database,
    IConfiguration configuration,
    ILogger<BackgroundHandler> logger,
    ScrapingHandler scrapingHandler,
    TCBScansSource tcbScansSource) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await UpdateSourcesInDatabaseAsync();

        var collection = database.GetCollection<SourceObject>("Sources");
        await Parallel.ForEachAsync(collection.FindAll(), stoppingToken, async (source, token) => {
            if (source.IsDisabled) {
                logger.LogWarning("Skipping disabled source {}", source.Name);
                return;
            }

            if (DateTime.Now.Subtract(source.UpdatedOn) < TimeSpan.FromMinutes(30)) {
                logger.LogDebug("Skipping {}; updated fetched less than 30 minutes ago.", source.Name);
                return;
            }

            logger.LogWarning("{} is outdated. Fetching latest information...", source.Name);
            if (source.Name.Contains("TCB")) {
                await tcbScansSource.GetMangasAsync();
            }
        });
    }

    private Task UpdateSourcesInDatabaseAsync() {
        var sources = configuration
            .GetSection("Sources")
            .Get<IReadOnlyCollection<SourceObject>>()!;
        logger.LogInformation("Current number of sources in config: {}", sources.Count);

        var collection = database.GetCollection<SourceObject>("Sources");
        var result = collection.InsertBulk(sources.Where(x => !collection.Exists(y => y.Id == x.Id)));
        logger.LogInformation("Inserted {} sources in database.", result);
        return Task.CompletedTask;
    }
}
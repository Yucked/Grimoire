using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources;
using Microsoft.Playwright;

namespace Grimoire.Services;

public sealed class MangaBackgroundService(
    IConfiguration configuration,
    ILogger<MangaBackgroundService> logger,
    DatabaseHandler databaseHandler,
    IBrowser browser,
    ServiceCoodrinator serviceCoodrinator,
    TCBScansSource tcbScansSource) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await serviceCoodrinator.WaitForServiceAsync(stoppingToken);
        await UpdateSourcesInDatabaseAsync();

        var sources = await databaseHandler.GetSourcesAysnc();
        while (browser.IsConnected && !stoppingToken.IsCancellationRequested) {
            await Parallel.ForEachAsync(sources, stoppingToken, async (source, token) => {
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
                    var mangaObjects = await tcbScansSource.GetMangasAsync();
                    if (mangaObjects.Count == 0) {
                        logger.LogWarning("Failed to fetch mangas from {}", source.Name);
                        return;
                    }

                    await databaseHandler.BulkStoreAsync(mangaObjects);
                }
            });
        }
    }

    private Task UpdateSourcesInDatabaseAsync() {
        var sources = configuration
            .GetSection("Sources")
            .Get<IReadOnlyCollection<SourceObject>>()!;
        logger.LogInformation("Current number of sources in config: {}", sources.Count);

        return databaseHandler.BulkStoreAsync(sources);
    }
}
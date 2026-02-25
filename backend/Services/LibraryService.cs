using Grimoire.Handlers;
using Grimoire.Sources;
using Microsoft.Playwright;

namespace Grimoire.Services;

public sealed class LibraryService(
    ILogger<LibraryService> logger,
    DatabaseHandler databaseHandler,
    IBrowser browser,
    ServiceCoodrinator serviceCoodrinator,
    TCBScansSource tcbScansSource) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await serviceCoodrinator.WaitForServiceAsync(stoppingToken);
        /*
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

                await Task.Delay(5000, token);
            });
        }
        */
    }

    // create dictionary of sources id : name
    // check 
}
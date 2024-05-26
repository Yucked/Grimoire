using Grimoire.Objects;
using Grimoire.Sources;
using LiteDB;
using Microsoft.Playwright;

namespace Grimoire.Handlers;

public sealed class BackgroundHandler(
    ILiteDatabase database,
    IConfiguration configuration,
    ILogger<BackgroundHandler> logger,
    ScrapingHandler scrapingHandler,
    IBrowser browser,
    TCBScansSource tcbScansSource) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await UpdateSourcesInDatabaseAsync();
        
        var collection = database.GetCollection<SourceObject>("Sources");
        while (browser.IsConnected && !stoppingToken.IsCancellationRequested) {
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
                    var mangaObjects = await tcbScansSource.GetMangasAsync();
                    if (mangaObjects.Count == 0) {
                        logger.LogWarning("Failed to fetch mangas from {}", source.Name);
                        return;
                    }
                    
                    var mangaSource = database.GetCollection<MangaObject>(source.Id);
                    mangaSource.InsertBulk(mangaObjects);
                }
            });
        }
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
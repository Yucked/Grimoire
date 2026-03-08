using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Services;

public sealed class LibraryBackgroundService(
    ILogger<LibraryBackgroundService> logger,
    DatabaseHandler databaseHandler,
    IEnumerable<IGrimoireSource> grimoireSources,
    ServiceCoordinator serviceCoordinator) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await serviceCoordinator.WaitForServiceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            var users = await databaseHandler.GetUsersAsync();
            var distinctMangaIds = users
                .SelectMany(u => u.Library)
                .Select(e => e.Key)
                .Distinct()
                .ToList();

            if (distinctMangaIds.Count == 0) {
                logger.LogDebug("No mangas in any user's library. Waiting 5 minutes...");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                continue;
            }

            await Parallel.ForEachAsync(distinctMangaIds, stoppingToken, async (mangaId, _) => {
                try {
                    var manga = await databaseHandler.GetMangaByIdAsync(mangaId);
                    if (manga == default) {
                        logger.LogWarning("Manga {} not found in database.", mangaId);
                        return;
                    }

                    if (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - manga.UpdatedAt.DayNumber < 5) {
                        logger.LogDebug("Skipping {mangaTitle}; fetched less than 5 days ago.", manga.Title);
                        return;
                    }

                    var source = grimoireSources.FirstOrDefault(s => s.Name.GetIdFromName() == manga.SourceId);
                    if (source is null) {
                        logger.LogWarning("No source found for SourceId {mangaSourceId} (manga: {mangaTitle}).",
                            manga.SourceId, manga.Title);
                        return;
                    }

                    logger.LogInformation("Refreshing {mangaTitle}...", manga.Title);
                    var updated = await source.GetMangaAsync(manga.SourceUrl);
                    await databaseHandler.StoreAsync(updated);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failed to refresh manga {}.", mangaId);
                }
            });

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
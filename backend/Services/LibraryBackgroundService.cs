using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Services;

public sealed class LibraryBackgroundService(
    ILogger<LibraryBackgroundService> logger,
    DatabaseHandler databaseHandler,
    ServiceCoordinator serviceCoordinator,
    IEnumerable<IGrimoireSource> sources) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await serviceCoordinator.WaitForServiceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            var users = await databaseHandler.GetUsersAsync();
            if (users.Count == 0) {
                logger.LogWarning("No users found.");
                return;
            }

            var library = users
                .SelectMany(user => user.Library)
                .Select(x => x.Key)
                .Distinct()
                .ToList();

            if (library.Count == 0) {
                logger.LogWarning("Libraries are empty for all users.");
                return;
            }

            await Parallel.ForEachAsync(library, stoppingToken, async (key, _) => {
                var sourceId = key.Split('/')[0];
                var mangaId = key.Split('/')[1];

                var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
                if (manga == null) {
                    logger.LogWarning("Manga {MangaId} not found.", mangaId);
                    return;
                }

                var source = sources.FirstOrDefault(x => x.Name.GetIdFromName() == sourceId);
                if (source is null) {
                    logger.LogWarning("Source {SourceId} not found.", sourceId);
                }

                var updated = await source.GetMangaAsync(manga.SourceUrl);
                await databaseHandler.StoreAsync(updated);
            });

            await Task.Delay(TimeSpan.FromDays(5), stoppingToken);
        }
    }
}
using Grimoire.Handlers;

namespace Grimoire.Services;

public sealed class ChapterDownloadService(
    ScrapingHandler scrapingHandler,
    DatabaseHandler databaseHandler,
    DownloadQueue downloadQueue,
    ILogger<ChapterDownloadService> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await foreach (var (sourceId, mangaId, chapterNumber, imageUrls) in
                       downloadQueue.Reader.ReadAllAsync(stoppingToken)) {
            if (imageUrls.Length <= 0) {
                logger.LogWarning("No image URLs for chapter {chapterNumber} of {mangaId}, skipping.", 
                    chapterNumber,
                    mangaId);
                continue;
            }

            try {
                var results = await Task.WhenAll(imageUrls.Select(async (url, i) => {
                    try {
                        return await scrapingHandler.SaveImageAsync(url, sourceId, mangaId);
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Failed to download page {pageNum} for chapter {chapterNumber}", i,
                            chapterNumber);
                        return null;
                    }
                }));

                var pages = results.Where(p => p is not null).Select(p => p!).ToArray();
                if (pages.Length == 0) {
                    logger.LogWarning("All pages failed for chapter {chapterNumber} of {mangaId}, skipping update.",
                        chapterNumber, mangaId);
                    continue;
                }

                await databaseHandler.UpdateChapterPagesAsync(sourceId, mangaId, chapterNumber, pages);
                logger.LogInformation("Downloaded {saved}/{total} pages for chapter {chapterNumber} of {mangaId}",
                    pages.Length, imageUrls.Length, chapterNumber, mangaId);
            }
            catch (Exception ex) {
                logger.LogError(ex, "Failed to process download job for chapter {chapterNumber} of {mangaId}",
                    chapterNumber, mangaId);
            }
        }
    }
}
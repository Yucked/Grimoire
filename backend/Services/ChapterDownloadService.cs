using System.Threading.Channels;
using Grimoire.Handlers;

namespace Grimoire.Services;

public sealed class ChapterDownloadService(
    ScrapingHandler scrapingHandler,
    DatabaseHandler databaseHandler,
    ILogger<ChapterDownloadService> logger) : BackgroundService {
    private readonly Channel<(string SourceId, string MangaId, string ChapterNumber, string[] ImageUrls)> _channel
        = Channel.CreateBounded<(string, string, string, string[])>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    public ValueTask EnqueueAsync(string sourceId, string mangaId, string chapterNumber, string[] imageUrls)
        => _channel.Writer.WriteAsync((sourceId, mangaId, chapterNumber, imageUrls));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await foreach (var (sourceId, mangaId, chapterNumber, imageUrls) in _channel.Reader.ReadAllAsync(stoppingToken))
            try {
                var pages = await Task.WhenAll(imageUrls.Select(async (url, i) => {
                    try {
                        return await scrapingHandler.SaveImageAsync(url, sourceId, mangaId);
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Failed to download page {pageNum} for chapter {chapterNumber}",
                            i, chapterNumber);
                        return url;
                    }
                }));

                await databaseHandler.UpdateChapterPagesAsync(sourceId, mangaId, chapterNumber, pages);
                logger.LogInformation("Completed download for chapter {chapterNumber} of {mangaId}",
                    chapterNumber, mangaId);
            }
            catch (Exception ex) {
                logger.LogError(ex, "Failed to process download job for chapter {chapterNumber} of {mangaId}",
                    chapterNumber, mangaId);
            }
    }
}
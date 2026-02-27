using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Services;
using Grimoire.Sources;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class MangaController(
    DatabaseHandler databaseHandler,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    DownloadQueue downloadQueue) : ControllerBase {

    [HttpGet("{sourceId}")]
    public async ValueTask<ResponseObject> GetMangasAsync(string sourceId,
        [FromQuery] int page = 0, [FromQuery] int pageSize = 25) {
        if (string.IsNullOrWhiteSpace(sourceId))
            return ResponseObject.New(StatusCodes.Status400BadRequest);

        var mangas = await databaseHandler.GetMangasAsync(sourceId, page, pageSize);
        if (mangas.Count is 0) {
            var source = serviceProvider.GetKeyedService<TCBScansSource>(sourceId);
            if (source is null)
                return ResponseObject.New(StatusCodes.Status404NotFound);

            mangas = await source.GetMangasAsync();
            await databaseHandler.BulkStoreAsync(mangas);
        }

        return mangas.Count == 0
            ? ResponseObject.New(StatusCodes.Status204NoContent)
            : await mangas.AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("{sourceId}/{mangaId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId) {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(mangaId))
            return ResponseObject.New(StatusCodes.Status400BadRequest);

        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        if (manga == default)
            return ResponseObject.New(StatusCodes.Status404NotFound);

        return ResponseObject.New(StatusCodes.Status200OK, manga);
    }

    [HttpGet("{sourceId}/{mangaId}/{chapterId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId, string chapterId) {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(mangaId) || string.IsNullOrWhiteSpace(chapterId))
            return ResponseObject.New(StatusCodes.Status400BadRequest);

        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        if (manga == default)
            return ResponseObject.New(StatusCodes.Status404NotFound);

        var chapter = manga.Chapters?.FirstOrDefault(x => x.Number == chapterId) ?? default;
        if (chapter.SourceUrl is null)
            return ResponseObject.New(StatusCodes.Status404NotFound);

        if (!chapter.IsDownloaded) {
            var source = serviceProvider.GetKeyedService<TCBScansSource>(manga.SourceId);
            if (source is not null) {
                chapter = await source.FetchChapterAsync(chapter, manga.SourceId, manga.Id);
                await databaseHandler.UpdateChapterAsync(sourceId, mangaId, chapter);

                if (configuration.GetValue<bool>("Library:DownloadChapters")) {
                    var imageUrls = chapter.Pages.Values
                        .Select(p => p.ImageUrl)
                        .Where(u => !string.IsNullOrEmpty(u))
                        .ToList()
                        .AsReadOnly();
                    await downloadQueue.EnqueueAsync(new ChapterDownloadJob(sourceId, mangaId, chapterId, imageUrls));
                }
            }
        }

        return await chapter.AsResponseAsync(StatusCodes.Status200OK);
    }
}

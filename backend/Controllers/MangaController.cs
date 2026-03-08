using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Services;
using Grimoire.Sources;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController]
[Route("api/[controller]/{sourceId}")]
[Produces("application/json")]
public sealed class MangaController(
    DatabaseHandler databaseHandler,
    IEnumerable<IGrimoireSource> sources,
    IConfiguration configuration,
    DownloadQueue downloadQueue) : ControllerBase {
    [HttpGet("")]
    public async ValueTask<ResponseObject> GetMangasAsync(string sourceId,
                                                          [FromQuery] int page = 0,
                                                          [FromQuery] int pageSize = 25) {
        if (string.IsNullOrWhiteSpace(sourceId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        var mangas = await databaseHandler.GetMangasAsync(sourceId, page, pageSize);
        if (mangas.Count is not 0) {
            return mangas.Count == 0
                ? ResponseObject.New(StatusCodes.Status204NoContent)
                : await mangas.AsResponseAsync(StatusCodes.Status200OK);
        }

        var source = sources.FirstOrDefault(s => s.Name.GetIdFromName() == sourceId);
        if (source is null) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        mangas = await source.GetMangasAsync();
        await databaseHandler.BulkStoreAsync(mangas);

        return mangas.Count == 0
            ? ResponseObject.New(StatusCodes.Status204NoContent)
            : await mangas.AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("{mangaId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId) {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(mangaId))
            return ResponseObject.New(StatusCodes.Status400BadRequest);

        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        return manga == default
            ? ResponseObject.New(StatusCodes.Status404NotFound)
            : ResponseObject.New(StatusCodes.Status200OK, manga);
    }

    [HttpGet("{mangaId}/{chapterId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId, string chapterId) {
        if (string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(mangaId) ||
            string.IsNullOrWhiteSpace(chapterId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        if (manga == default) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        var chapter = manga.Chapters?.FirstOrDefault(x => x.Number == chapterId) ?? default;
        if (string.IsNullOrWhiteSpace(chapter.SourceUrl)) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        if (chapter.IsDownloaded) {
            return await chapter.AsResponseAsync(StatusCodes.Status200OK);
        }

        var source = sources.FirstOrDefault(s => s.Name.GetIdFromName() == manga.SourceId);
        if (source is not null) {
            chapter = await source.FetchChapterAsync(chapter, manga.SourceId, manga.Id);
            await databaseHandler.UpdateChapterAsync(sourceId, mangaId, chapter);
        }

        if (configuration.GetValue<bool>("Library:DownloadChapters")) {
            var imageUrls = chapter.Pages.Values
                .Select(p => p.ImageUrl)
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList()
                .AsReadOnly();
            await downloadQueue.EnqueueAsync(new ChapterDownloadJob(sourceId, mangaId, chapterId, imageUrls));
        }

        return await chapter.AsResponseAsync(StatusCodes.Status200OK);
    }
}
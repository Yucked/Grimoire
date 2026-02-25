using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class MangaController(DatabaseHandler databaseHandler,
    IServiceProvider serviceProvider) : ControllerBase {

    [HttpGet("{sourceId}")]
    public async ValueTask<ResponseObject> GetMangasAsync(string sourceId) {
        var mangas = await databaseHandler.GetMangasAsync(sourceId);
        if (mangas.Count is 0) {
            var source = serviceProvider.GetKeyedService<TCBScansSource>(sourceId);
            mangas = await source.GetMangasAsync();
            await databaseHandler.BulkStoreAsync(mangas);
        }

        return mangas.Count == 0
            ? ResponseObject.New(StatusCodes.Status204NoContent)
            : await mangas.AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("{sourceId}/{mangaId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId) {
        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        if (manga == default) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        return ResponseObject.New(StatusCodes.Status200OK, manga);
    }

    [HttpGet("{chapterId:int}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId, string chapterId) {
        var manga = await databaseHandler.GetMangaAsync(sourceId, mangaId);
        if (manga == default) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }
        return await manga
            .Chapters
            .First(x => x.Number == chapterId)
            .AsResponseAsync(StatusCodes.Status200OK);
    }
}
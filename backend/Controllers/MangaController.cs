using Grimoire.Handlers;
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]/{mangaId}"),
 Produces("application/json")]
public sealed class MangaController(DatabaseHandler databaseHandler) : ControllerBase {
    [HttpGet("")]
    public async ValueTask<ResponseObject> GetAsync(string mangaId, [FromQuery] string sourceId) {
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
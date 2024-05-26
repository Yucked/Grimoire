using Grimoire.Objects;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]/{mangaId}"),
 Produces("application/json")]
public sealed class MangaController(ILiteDatabase database) : ControllerBase {
    [HttpGet("")]
    public async ValueTask<ResponseObject> GetAsync(string mangaId, [FromQuery] string sourceId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        if (!collection.Exists(x => x.Id == mangaId)) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        return await collection
            .FindById(mangaId)
            .AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("{chapterId:int}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId, string mangaId, string chapterId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        if (!collection.Exists(x => x.Id == mangaId)) {
            return ResponseObject.New(StatusCodes.Status404NotFound);
        }

        return await collection
            .FindById(mangaId)
            .Chapters
            .First(x => x.Number == chapterId)
            .AsResponseAsync(StatusCodes.Status200OK);
    }
}
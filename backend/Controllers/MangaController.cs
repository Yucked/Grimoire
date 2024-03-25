using Grimoire.Objects;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]/{sourceId}/{mangaId}"),
 Produces("application/json")]
public sealed class MangaController(ILiteDatabase database) : ControllerBase {
    [HttpGet("")]
    public async ValueTask<RestResponse> GetAsync(string sourceId, string mangaId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        if (!collection.Exists(x => x.Id == mangaId)) {
            return RestResponse.New(StatusCodes.Status404NotFound);
        }

        return collection
            .FindById(mangaId)
            .AsResponse(StatusCodes.Status200OK);
    }

    [HttpGet("{chapterId:int}")]
    public async ValueTask<RestResponse> GetAsync(string sourceId, string mangaId, int chapterId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        if (!collection.Exists(x => x.Id == mangaId)) {
            return RestResponse.New(StatusCodes.Status404NotFound);
        }

        return collection
            .FindById(mangaId)
            .Chapters
            .First(x => x.Number == chapterId)
            .AsResponse(StatusCodes.Status200OK);
    }
}
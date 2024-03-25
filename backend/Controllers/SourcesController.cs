using Grimoire.Objects;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class SourcesController(ILiteDatabase database) : ControllerBase {
    [HttpGet]
    public async ValueTask GetAsync() {
        // return all sources
    }

    [HttpGet("{sourceId}")]
    public async ValueTask<RestResponse> GetAsync(string sourceId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        if (collection == null) {
            return RestResponse.New(StatusCodes.Status404NotFound);
        }

        return collection
            .FindAll()
            .AsResponse(StatusCodes.Status200OK);
    }
}
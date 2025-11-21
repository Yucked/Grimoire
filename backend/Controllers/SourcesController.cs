using Grimoire.Handlers;
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class SourcesController(
    DatabaseHandler databaseHandler) : ControllerBase {
    [HttpGet]
    public async ValueTask<ResponseObject> GetAsync() {
        var sources = await databaseHandler.GetSourcesAysnc();
        return ResponseObject.New(StatusCodes.Status200OK, sources);
    }

    [HttpGet("{sourceId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId) {
        var source = await databaseHandler.GetMangasAsync(sourceId);
        return source.Count == 0
            ? ResponseObject.New(StatusCodes.Status404NotFound)
            : await source.AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpPut]
    public async ValueTask<ResponseObject> PutAsync(SourceObject source) {
        await databaseHandler.StoreAsync(source);
        return ResponseObject.New(StatusCodes.Status200OK);
    }
}
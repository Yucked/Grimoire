using Grimoire.Handlers;
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class SourceController(
    DatabaseHandler databaseHandler) : ControllerBase {
    [HttpGet]
    public async ValueTask<ResponseObject> GetAsync() {
        var sources = await databaseHandler.GetSourcesAsync();
        return sources.Count == 0
            ? ResponseObject.New(StatusCodes.Status204NoContent)
            : ResponseObject.New(StatusCodes.Status200OK, sources);
    }

    [HttpGet("{sourceId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId) {
        var source = await databaseHandler.GetSourceAsync(sourceId);
        return ResponseObject.New(StatusCodes.Status200OK, source);
    }

    [HttpPost]
    public async ValueTask<ResponseObject> AddAsync(SourceObject source) {
        await databaseHandler.StoreAsync(source);
        return ResponseObject.New(StatusCodes.Status200OK);
    }
}
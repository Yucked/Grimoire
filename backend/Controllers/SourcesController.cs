using Grimoire.Handlers;
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class SourcesController(
    DatabaseHandler databaseHandler,
    IConfiguration configuration) : ControllerBase {
    [HttpGet]
    public ValueTask<ResponseObject> GetAsync() {
        return configuration
            .GetSection("Sources")
            .Get<IReadOnlyCollection<SourceObject>>()!
            .AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("{sourceId}")]
    public async ValueTask<ResponseObject> GetAsync(string sourceId) {
        var source = await databaseHandler.GetSourceAsync(sourceId);
        return source.Count == 0
            ? ResponseObject.New(StatusCodes.Status404NotFound)
            : await source.AsResponseAsync(StatusCodes.Status200OK);
    }
}
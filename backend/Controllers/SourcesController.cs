using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class SourcesController : ControllerBase {
    [HttpGet]
    public async ValueTask GetAsync() {
        // return all sources
    }

    [HttpGet("{sourceId}")]
    public async ValueTask GetAsync(string sourceId) {
        // list all mangas in a source
    }
}
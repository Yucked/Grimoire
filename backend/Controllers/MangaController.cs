using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]/{sourceId}/{mangaId}"),
 Produces("application/json")]
public class MangaController : ControllerBase {
    [HttpGet("")]
    public async ValueTask GetAsync(string sourceId, string mangaId) {
        // return manga from source
    }

    [HttpGet("{chapterId:int}")]
    public async ValueTask GetAsync(string sourceId, string mangaId, int chapterId) {
        // return chapter of manga
    }
}
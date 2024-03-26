using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class GeneralController : ControllerBase {
    [HttpGet]
    public ValueTask<ResponseObject> PingAsync() {
        return DateTime.Now.AsResponseAsync(StatusCodes.Status200OK);
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class GeneralController : ControllerBase {
    [HttpGet]
    public ValueTask<Response<DateTime>> PingAsync() {
        return ValueTask.FromResult(Response<DateTime>.New(StatusCodes.Status200OK, DateTime.Now));
    }
}
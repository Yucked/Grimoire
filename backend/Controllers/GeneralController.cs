using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]"),
 Produces("application/json")]
public sealed class GeneralController : ControllerBase {
    [HttpGet]
    public ValueTask<RestResponse> PingAsync() {
        return ValueTask.FromResult(RestResponse.New(StatusCodes.Status200OK, DateTime.Now));
    }
}
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;

namespace Grimoire.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class GeneralController(
    IDocumentStore documentStore,
    IMinioClient minioClient) : ControllerBase {
    [HttpGet]
    public ValueTask<ResponseObject> PingAsync() {
        return DateTime.Now.AsResponseAsync(StatusCodes.Status200OK);
    }

    [HttpGet("health")]
    public async ValueTask<ResponseObject> HealthAsync() {
        var ravenStatus = "healthy";
        var minioStatus = "healthy";

        try {
            await documentStore.Maintenance.Server.SendAsync(new GetDatabaseNamesOperation(0, 1));
        }
        catch {
            ravenStatus = "unhealthy";
        }

        try {
            await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket("health-check"));
        }
        catch {
            minioStatus = "unhealthy";
        }

        var payload = new {
            ravendb = ravenStatus,
            minio = minioStatus
        };

        return ravenStatus == "unhealthy" || minioStatus == "unhealthy"
            ? ResponseObject.New(StatusCodes.Status503ServiceUnavailable, payload)
            : ResponseObject.New(StatusCodes.Status200OK, payload);
    }
}
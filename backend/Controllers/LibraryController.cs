using Grimoire.Handlers;
using Grimoire.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Controllers;

[ApiController,
 Route("api/[controller]/{userId}"),
 Produces("application/json")]
public sealed class LibraryController(
    DatabaseHandler databaseHandler) : ControllerBase {
    [HttpGet]
    public async ValueTask<ResponseObject> GetLibraryAsync(string userId) {
        if (string.IsNullOrWhiteSpace(userId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        var user = await databaseHandler.GetUserAsync($"users/{userId}");
        return user is null
            ? ResponseObject.New(StatusCodes.Status404NotFound)
            : ResponseObject.New(StatusCodes.Status200OK, user);
    }

    [HttpPost("{sourceId}/{mangaId}")]
    public async ValueTask<ResponseObject> AddToLibraryAsync(string userId, string sourceId, string mangaId) {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(mangaId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        await databaseHandler.AddToLibraryAsync($"users/{userId}", $"mangas/{sourceId}/{mangaId}");
        return ResponseObject.New(StatusCodes.Status200OK);
    }

    [HttpDelete("{sourceId}/{mangaId}")]
    public async ValueTask<ResponseObject> RemoveFromLibraryAsync(string userId, string sourceId, string mangaId) {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(mangaId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        await databaseHandler.RemoveFromLibraryAsync($"users/{userId}", $"mangas/{sourceId}/{mangaId}");
        return ResponseObject.New(StatusCodes.Status200OK);
    }

    [HttpPatch("{sourceId}/{mangaId}")]
    public async ValueTask<ResponseObject> UpdateProgressAsync(string userId, string sourceId, string mangaId,
                                                               [FromBody] ProgressUpdateRequest request) {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(mangaId)) {
            return ResponseObject.New(StatusCodes.Status400BadRequest);
        }

        await databaseHandler.UpdateProgressAsync(
            $"users/{userId}",
            $"mangas/{sourceId}/{mangaId}",
            request.LastChapterRead,
            request.LastPageRead);
        return ResponseObject.New(StatusCodes.Status200OK);
    }
}

public record ProgressUpdateRequest(float LastChapterRead, int LastPageRead);
using LiteDB;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Handlers;

public sealed class DatabaseHandler(
    ILiteDatabase database,
    IMemoryCache memoryCache,
    ILogger<DatabaseHandler> logger)
{
    public async Task GetMangaAsync(string sourceId, string mangaId)
    {
        
    }
}
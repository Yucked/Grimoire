using Grimoire.Objects;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Handlers;

public sealed class DatabaseHandler(
    ILiteDatabase database,
    IMemoryCache memoryCache,
    IConfiguration configuration,
    ILogger<DatabaseHandler> logger) {
    public ValueTask<IReadOnlyCollection<MangaObject>> GetSourceAsync(string sourceId) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        return ValueTask.FromResult<IReadOnlyCollection<MangaObject>>(collection.FindAll().ToArray());
    }

    public async Task ListSourcesAsync() { }

    public async Task GetMangaAsync(string sourceId, string mangaId) { }

    public async Task GetMangaChapterAsync(string sourceId, string mangaId, int chapterId) { }

    public ValueTask<IReadOnlyCollection<MangaObject>> SearchSourceAsync(string sourceId, string query) {
        var collection = database.GetCollection<MangaObject>(sourceId);
        var results = collection.Find(x
            => x.Title.Equals(query, StringComparison.CurrentCultureIgnoreCase)
               || x.Title.Contains(query)
               || x.Summary.Equals(query, StringComparison.CurrentCultureIgnoreCase)
               || x.Summary.Contains(query)
               || x.Aliases.Any(y
                   => y.Contains(query)
                      || y.Equals(query, StringComparison.CurrentCultureIgnoreCase))
               || x.Artists.Any(y
                   => y.Contains(query)
                      || y.Equals(query, StringComparison.CurrentCultureIgnoreCase))
               || x.Genres.Any(y
                   => y.Contains(query)
                      || y.Equals(query, StringComparison.CurrentCultureIgnoreCase))
               || x.Authors.Any(y
                   => y.Contains(query)
                      || y.Equals(query, StringComparison.CurrentCultureIgnoreCase))
        );
        return ValueTask.FromResult<IReadOnlyCollection<MangaObject>>(results.ToArray());
    }

    public async ValueTask<IReadOnlyCollection<MangaObject>> SearchAllSourcesAsync(string query) {
        var tasks = database.GetCollectionNames()
            .Select(x => SearchSourceAsync(x, query).AsTask());
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).ToArray();
    }
}
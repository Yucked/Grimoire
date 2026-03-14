using Grimoire.Objects;
using Raven.Client.Documents;

namespace Grimoire.Handlers;

public sealed partial class DatabaseHandler {
    public async Task<IReadOnlyCollection<SourceObject>> GetSourcesAsync() {
        using var session = documentStore.OpenAsyncSession();
        return await session
            .Query<SourceObject>()
            .ToListAsync();
    }

    public async Task<SourceObject> GetSourceAsync(string sourceId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<SourceObject>(sourceId);
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchSourceAsync(string sourceId, string query) {
        using var session = documentStore.OpenAsyncSession();
        var results = session.Query<MangaObject, MangaSearchIndex>()
            .Where(x => x.SourceId == sourceId)
            .Search(x => x.Title, query, 10)
            .Search(x => x.Summary, query, 8)
            .Search(x => x.Aliases, query, 6)
            .Search(x => x.Genres, query, 4)
            .Search(x => x.Authors, query, 2);
        return await results.ToListAsync();
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchAllSourcesAsync(string query) {
        using var session = documentStore.OpenAsyncSession();
        var results = session.Query<MangaObject, MangaSearchIndex>()
            .Search(x => x.Title, query, 10)
            .Search(x => x.Summary, query, 8)
            .Search(x => x.Aliases, query, 6)
            .Search(x => x.Genres, query, 4)
            .Search(x => x.Authors, query, 2);
        return await results.ToListAsync();
    }

    public async Task<bool> ToggleSourceAsync(string sourceId) {
        using var session = documentStore.OpenAsyncSession();
        var source = await session.LoadAsync<SourceObject>(sourceId);
        if (source is null) {
            return false;
        }

        source.IsDisabled = !source.IsDisabled;
        await session.SaveChangesAsync();
        return source.IsDisabled;
    }
}
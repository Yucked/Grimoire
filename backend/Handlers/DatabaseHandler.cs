using Grimoire.Objects;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace Grimoire.Handlers;

public sealed class DatabaseHandler(IDocumentStore documentStore) {
    public async Task<IReadOnlyCollection<SourceObject>> GetSourcesAysnc() {
        var session = documentStore.OpenAsyncSession();
        return await session
            .Query<SourceObject>()
            .ToListAsync();
    }

    public async Task<SourceObject> GetSourceAysnc(string sourceId) {
        var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<SourceObject>(sourceId);
    }

    public async Task<IReadOnlyCollection<MangaObject>> GetMangasAsync(string sourceId) {
        var session = documentStore.OpenAsyncSession();
        return await session
            .Query<MangaObject>()
            .Where(x => x.SourceId == sourceId)
            .ToListAsync();
    }

    public async Task<MangaObject> GetMangaAsync(string sourceId, string mangaId) {
        var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<MangaObject>($"mangas/{sourceId}/{mangaId}");
    }

    public async Task<ChapterObject> GetMangaChapterAsync(string sourceId, string mangaId, string chapterId) {
        var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"mangas/{sourceId}/{mangaId}");
        return manga
            .Chapters
            .First(x => x.Number == chapterId);
    }

    public async Task StoreAsync<T>(T item) {
        var session = documentStore.OpenAsyncSession();
        if (item is MangaObject mangaObject) {
            await session.StoreAsync(mangaObject, mangaObject.Id);
        }
        else if (item is SourceObject sourceObject) {
            await session.StoreAsync(sourceObject, sourceObject.Id);
        }
        await session.SaveChangesAsync();
    }

    public async Task BulkStoreAsync<T>(IReadOnlyCollection<T> items) {
        BulkInsertOperation bulkInsert = null;
        try {
            bulkInsert = documentStore.BulkInsert();
            await Task.Run(async () => {
                foreach (var item in items) {
                    await bulkInsert.StoreAsync(item);
                }
            });
        }
        finally {
            if (bulkInsert != null) {
                await bulkInsert.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchSourceAsync(string sourceId, string query) {
        var session = documentStore.OpenAsyncSession();
        var dbQuery = session.Query<MangaObject, MangaSearchIndex>()
            .Where(x => x.SourceId == sourceId);
        var results = dbQuery
            .Search(x => x.Title, query, boost: 10)
            .Search(x => x.Summary, query, boost: 8)
            .Search(x => x.Aliases, query, boost: 6)
            .Search(x => x.Genres, query, boost: 4)
            .Search(x => x.Authors, query, boost: 2);
        return await results.ToListAsync();
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchAllSourcesAsync(string query) {
        throw new NotImplementedException();

    }

    public void StoreImage(string sourceId, string mangaId, string g, Stream stream) {
        throw new NotImplementedException();
    }
}
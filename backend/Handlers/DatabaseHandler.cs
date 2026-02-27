using Grimoire.Objects;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace Grimoire.Handlers;

public sealed class DatabaseHandler(IDocumentStore documentStore) {
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

    public async Task<IReadOnlyCollection<MangaObject>> GetMangasAsync(string sourceId, int page = 0, int pageSize = 25) {
        using var session = documentStore.OpenAsyncSession();
        return await session
            .Query<MangaObject>()
            .Where(x => x.SourceId == sourceId)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<MangaObject> GetMangaAsync(string sourceId, string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<MangaObject>($"mangas/{sourceId}/{mangaId}");
    }

    public async Task<ChapterObject> GetMangaChapterAsync(string sourceId, string mangaId, string chapterId) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"mangas/{sourceId}/{mangaId}");
        return manga
            .Chapters
            .FirstOrDefault(x => x.Number == chapterId);
    }

    public async Task StoreAsync<T>(T item) {
        using var session = documentStore.OpenAsyncSession();
        if (item is MangaObject mangaObject) {
            await session.StoreAsync(mangaObject, $"mangas/{mangaObject.Id}");
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
            foreach (var item in items) {
                var id = item switch {
                    MangaObject manga => $"mangas/{manga.Id}",
                    SourceObject source => source.Id,
                    _ => null
                };
                await bulkInsert.StoreAsync(item, id);
            }
        }
        finally {
            if (bulkInsert != null) {
                await bulkInsert.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchSourceAsync(string sourceId, string query) {
        using var session = documentStore.OpenAsyncSession();
        var results = session.Query<MangaObject, MangaSearchIndex>()
            .Where(x => x.SourceId == sourceId)
            .Search(x => x.Title, query, boost: 10)
            .Search(x => x.Summary, query, boost: 8)
            .Search(x => x.Aliases, query, boost: 6)
            .Search(x => x.Genres, query, boost: 4)
            .Search(x => x.Authors, query, boost: 2);
        return await results.ToListAsync();
    }

    public async Task<IReadOnlyCollection<MangaObject>> SearchAllSourcesAsync(string query) {
        using var session = documentStore.OpenAsyncSession();
        var results = session.Query<MangaObject, MangaSearchIndex>()
            .Search(x => x.Title, query, boost: 10)
            .Search(x => x.Summary, query, boost: 8)
            .Search(x => x.Aliases, query, boost: 6)
            .Search(x => x.Genres, query, boost: 4)
            .Search(x => x.Authors, query, boost: 2);
        return await results.ToListAsync();
    }

    public async Task<ReadingProgressObject?> GetProgressAsync(string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<ReadingProgressObject?>($"progress/{mangaId}");
    }

    public async Task UpsertProgressAsync(ReadingProgressObject progress) {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(progress, progress.Id);
        await session.SaveChangesAsync();
    }

    public async Task DeleteProgressAsync(string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        session.Delete($"progress/{mangaId}");
        await session.SaveChangesAsync();
    }

    public async Task UpdateChapterAsync(string sourceId, string mangaId, ChapterObject chapter) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"mangas/{sourceId}/{mangaId}");
        if (manga == default) return;

        var chapters = manga.Chapters.ToList();
        var index = chapters.FindIndex(x => x.Number == chapter.Number);
        if (index >= 0) chapters[index] = chapter;

        manga = manga with { Chapters = chapters };
        await session.StoreAsync(manga, $"mangas/{manga.Id}");
        await session.SaveChangesAsync();
    }
}

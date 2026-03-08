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

    public async Task<IReadOnlyCollection<MangaObject>>
        GetMangasAsync(string sourceId, int page = 0, int pageSize = 25) {
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
        return await session.LoadAsync<MangaObject>($"{sourceId}/{mangaId}");
    }

    public async Task<MangaObject> GetMangaByIdAsync(string fullId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<MangaObject>(fullId);
    }

    public async Task<ChapterObject> GetMangaChapterAsync(string sourceId, string mangaId, string chapterId) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"{sourceId}/{mangaId}");
        return manga
            .Chapters
            .FirstOrDefault(x => x.Number == chapterId);
    }

    public async Task StoreAsync<T>(T item) {
        using var session = documentStore.OpenAsyncSession();
        var task = item switch {
            MangaObject manga   => session.StoreAsync(manga, $"{manga.Id}"),
            SourceObject source => session.StoreAsync(source, source.Id),
            UserObject user     => session.StoreAsync(user, user.Id),
            _                   => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };

        await task;
        await session.SaveChangesAsync();
    }

    public async Task BulkStoreAsync<T>(IReadOnlyCollection<T> items) {
        BulkInsertOperation? bulkInsert = null;
        try {
            bulkInsert = documentStore.BulkInsert();
            foreach (var item in items) {
                var id = item switch {
                    MangaObject manga   => $"{manga.Id}",
                    SourceObject source => source.Id,
                    _                   => null
                };
                await bulkInsert.StoreAsync(item, id);
            }
        }
        finally {
            if (bulkInsert != null) await bulkInsert.DisposeAsync();
        }
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

    public async Task UpdateChapterAsync(string sourceId, string mangaId, ChapterObject chapter) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"{sourceId}/{mangaId}");
        if (manga is null) {
            return;
        }

        var chapters = manga.Chapters.ToList();
        var index = chapters.FindIndex(x => x.Number == chapter.Number);
        if (index >= 0) {
            chapters[index] = chapter;
        }

        manga.Chapters = chapters;
        await session.SaveChangesAsync();
    }

    public async Task UpdateChapterPagesAsync(string sourceId, string mangaId, string chapterNumber, string[] pages) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"{sourceId}/{mangaId}");
        if (manga is null) {
            return;
        }

        var chapters = manga.Chapters.ToList();
        var index = chapters.FindIndex(x => x.Number == chapterNumber);
        if (index < 0) {
            return;
        }

        chapters[index] = chapters[index] with { IsDownloaded = true, Pages = pages };
        manga.Chapters = chapters;
        await session.SaveChangesAsync();
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

    // ── User / Library ────────────────────────────────────────────────────────

    public async Task<UserObject?> GetUserAsync(string userId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<UserObject?>(userId);
    }

    public async Task<IReadOnlyList<UserObject>> GetUsersAsync() {
        using var session = documentStore.OpenAsyncSession();
        return await session
            .Query<UserObject>()
            .ToListAsync();
    }

    public async Task UpsertUserAsync(UserObject user) {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(user, user.Id);
        await session.SaveChangesAsync();
    }

    public async Task AddToLibraryAsync(string userId, string sourceId, string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null ||
            user.Library.Any(x => x.Key == $"{sourceId}/{mangaId}") ||
            !user.Library.TryAdd(mangaId, 0)) {
            return;
        }

        await session.SaveChangesAsync();
    }

    public async Task RemoveFromLibraryAsync(string userId, string sourceId, string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null || !user.Library.TryRemove($"{sourceId}/{mangaId}", out _)) {
            return;
        }

        await session.SaveChangesAsync();
    }

    public async Task UpdateProgressAsync(string userId, string sourceId, string mangaId, float chapter) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null || !user.Library.TryGetValue($"{sourceId}/{mangaId}", out _)) {
            return;
        }

        user.Library.TryUpdate($"{sourceId}/{mangaId}", chapter, chapter);
        await session.SaveChangesAsync();
    }
}
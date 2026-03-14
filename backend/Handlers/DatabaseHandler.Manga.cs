using Grimoire.Objects;
using Raven.Client.Documents;

namespace Grimoire.Handlers;

public sealed partial class DatabaseHandler {
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

    public async Task<ChapterObject> GetMangaChapterAsync(string sourceId, string mangaId, string chapterId) {
        using var session = documentStore.OpenAsyncSession();
        var manga = await session.LoadAsync<MangaObject>($"{sourceId}/{mangaId}");
        return manga
            .Chapters
            .FirstOrDefault(x => x.Number == chapterId);
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

        chapters[index] = chapters[index] with { Pages = pages };
        manga.Chapters = chapters;
        await session.SaveChangesAsync();
    }
}
using Grimoire.Objects;

namespace Grimoire.Sources;

public interface IGrimoireSource {
    string Name { get; }
    string Url { get; }
    string Icon { get; }

    Task<IReadOnlyList<MangaObject>> GetMangasAsync();

    Task<MangaObject> GetMangaAsync(string url);

    Task<MangaObject> GetMangaAsync(MangaObject manga)
        => GetMangaAsync(manga.SourceUrl);

    Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId);
}
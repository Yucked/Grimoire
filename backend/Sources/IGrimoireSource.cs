using Grimoire.Objects;

namespace Grimoire.Sources;

/// <summary>
/// 
/// </summary>
public interface IGrimoireSource {
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyList<MangaObject>> GetMangasAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<MangaObject> GetMangaAsync(string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="manga"></param>
    /// <returns></returns>
    Task<MangaObject> GetMangaAsync(MangaObject manga)
        => GetMangaAsync(manga.SourceUrl);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chapter"></param>
    /// <returns></returns>
    Task<ChapterObject> FetchChapterAsync(ChapterObject chapter);
}
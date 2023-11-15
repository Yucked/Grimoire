using Grimoire.Helpers;
using Grimoire.Models;

namespace Grimoire.Sources.Interfaces;

/// <summary>
/// 
/// </summary>
public interface IGrimoireSource {
    /// <summary>
    /// 
    /// </summary>
    public string Id
        => Name.GetIdFromName();

    /// <summary>
    /// Name of the source
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Base URL of the source
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Favicon/Icon of the source
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyList<Manga>?> GetMangasAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<Manga?> GetMangaAsync(string url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="manga"></param>
    /// <returns></returns>
    Task<Manga?> GetMangaAsync(Manga? manga)
        => GetMangaAsync(manga!.Url);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chapter"></param>
    /// <returns></returns>
    Task<Chapter> FetchChapterAsync(Chapter chapter);
}
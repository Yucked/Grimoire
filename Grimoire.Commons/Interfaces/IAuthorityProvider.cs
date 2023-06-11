using Grimoire.Commons.Models;

namespace Grimoire.Commons.Interfaces;

public interface IGrimoireProvider {
    /// <summary>
    /// Name of the provider
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Base URL of the provider
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Favicon/Icon of the provider
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyList<Manga>> FetchMangasAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyList<Manga>> PaginateAsync(int page);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="manga"></param>
    /// <returns></returns>
    Task<IReadOnlyList<MangaChapter>> FetchChaptersAsync(Manga manga);
}
using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Abstractions;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public sealed class PornwaClubSource(
    ILogger<PornwaClubSource> logger,
    HttpHandler httpHandler) : IGrimoireSource {
    public string Name
        => "Pornwa Club";

    public string Url
        => "https://pornwa.club";

    public string Icon
        => $"{Url}/favicon1.ico";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return HanmaAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga> GetMangaAsync(string url) {
        return HanmaAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangaAsync(url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return HanmaAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .FetchChapterAsync(chapter);
    }
}
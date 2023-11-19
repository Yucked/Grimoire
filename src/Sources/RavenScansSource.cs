using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Abstractions;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

// "img.ts-main-image"
public class RavenScansSource(
    ILogger<RavenScansSource> logger,
    HttpHandler httpHandler) : IGrimoireSource {
    public string Name
        => "Raven Scans";

    public string Url
        => "https://ravenscans.com";

    public string Icon
        => "https://i0.wp.com/ravenscans.com/wp-content/uploads/2022/12/cropped-33.png";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return WordPressAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga> GetMangaAsync(string url) {
        return WordPressAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangaAsync(url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return WordPressAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .FetchChapterAsync(chapter);
    }
}
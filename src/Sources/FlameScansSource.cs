using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Abstractions;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class FlameScansSource(
    ILogger<FlameScansSource> logger,
    HttpHandler httpHandler) : IGrimoireSource {
    public string Name
        => "Flame Scans";

    public string Url
        => "https://flamecomics.com";

    public string Icon
        => $"{Url}/favicon.ico";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return WordPressAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangasAsync("series", true);
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
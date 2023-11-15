using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Abstractions;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class ArenaScansSource(
    ILogger<ArenaScansSource> logger,
    HttpHandler httpHandler) : IGrimoireSource {
    public string Name
        => "Arena Scans";

    public string Url
        => "https://team11x11.fun/";

    public string Icon
        => $"{Url}/favicon.ico";

    public Task<IReadOnlyList<Manga>?> GetMangasAsync() {
        return WordPressAbstraction
            .Helper(logger, httpHandler, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga?> GetMangaAsync(string url) {
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
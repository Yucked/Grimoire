using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Grimoire.Sources.Helpers;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class ArenaScansSource(
    ILogger<ArenaScansSource> logger,
    HtmlParser htmlParser) : IGrimoireSource {
    public string Name
        => "Arena Scans";

    public string Url
        => "https://team11x11.fun/";

    public string Icon
        => $"{Url}/favicon.ico";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga> GetMangaAsync(string url) {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .GetMangaAsync(url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .FetchChapterAsync(chapter);
    }
}